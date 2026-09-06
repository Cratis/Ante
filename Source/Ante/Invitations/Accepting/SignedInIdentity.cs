// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Ante.IdentityProviders;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting;

/// <summary>
/// Resolves the identity a person is onboarding under - the identity provider and the compliance
/// subject events are appended with.
/// </summary>
public interface ISignedInIdentity
{
    /// <summary>
    /// Resolves the identity provider and compliance subject for the current request.
    /// </summary>
    /// <param name="invitationId">The invitation being accepted, or <see cref="InvitationId.NotSet"/> for self-service registration.</param>
    /// <param name="fallbackSubject">The subject to fall back to when the request itself carries none.</param>
    /// <returns>The resolved identity provider and compliance subject.</returns>
    (IdentityProviderName Provider, Cratis.Chronicle.Subject Subject) Resolve(InvitationId invitationId, Cratis.Chronicle.Subject fallbackSubject);

    /// <summary>
    /// Resolves the identity provider for the current request.
    /// </summary>
    /// <returns>The resolved identity provider.</returns>
    IdentityProviderName ResolveProvider();
}

/// <summary>
/// Represents an implementation of <see cref="ISignedInIdentity"/> that resolves from the current HTTP
/// request first, falling back to the session recorded when the invitation was exchanged.
/// </summary>
/// <remarks>
/// The identity a person onboards under has to be the one they are signed in with right now - anything
/// else records somebody else's login against their new account, and they are then denied access to the
/// very organization they just created. So the current request decides, and the exchange session
/// captured when they authenticated only fills in what the request cannot say for itself.
/// </remarks>
/// <param name="httpContextAccessor">Accessor for the current HTTP request.</param>
/// <param name="acceptedInvitations">Collection of accepted invitation sessions.</param>
/// <param name="identityProviderResolver">Resolver used to normalize the reported identity provider.</param>
public class SignedInIdentity(
    IHttpContextAccessor httpContextAccessor,
    IMongoCollection<AcceptedInvitation> acceptedInvitations,
    IIdentityProviderResolver identityProviderResolver) : ISignedInIdentity
{
    // The authentication proxy stamps these onto the forwarded principal once it is configured to
    // resolve a canonical federated identity, and they are the only request-time signals that name the
    // provider behind a federated sign-in: the OpenID Connect handler deletes the `iss` claim by
    // default, and every federated identity is otherwise named after the same authentication type.
    const string CanonicalProviderKeyClaim = "urn:cratis:identity:provider-key";
    const string CanonicalIssuerClaim = "urn:cratis:identity:issuer";
    const string CanonicalSubjectClaim = "urn:cratis:identity:subject";

    /// <inheritdoc/>
    public (IdentityProviderName Provider, Cratis.Chronicle.Subject Subject) Resolve(InvitationId invitationId, Cratis.Chronicle.Subject fallbackSubject)
    {
        var subject = SubjectOfCurrentRequest();
        var session = SessionFor(invitationId, subject);

        return (ProviderOf(session), SubjectOf(subject, session, fallbackSubject));
    }

    /// <inheritdoc/>
    public IdentityProviderName ResolveProvider()
    {
        var subject = SubjectOfCurrentRequest();

        return ProviderOf(SessionFor(InvitationId.NotSet, subject));
    }

    static Cratis.Chronicle.Subject SubjectOf(string? subject, AcceptedInvitation? session, Cratis.Chronicle.Subject fallbackSubject)
    {
        var resolved = subject ?? (string.IsNullOrWhiteSpace(session?.Subject) ? null : session.Subject);

        return resolved is null ? fallbackSubject : new Cratis.Chronicle.Subject(resolved);
    }

    string? SubjectOfCurrentRequest()
    {
        var httpContext = httpContextAccessor.HttpContext;
        var user = httpContext?.User;

        var subject = user?.FindFirstValue(CanonicalSubjectClaim)
            ?? httpContext?.Request.Headers[Cratis.Arc.Identity.MicrosoftIdentityPlatformHeaders.IdentityIdHeader].FirstOrDefault()
            ?? user?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user?.FindFirstValue("sub");

        return string.IsNullOrWhiteSpace(subject) ? null : subject;
    }

    IdentityProviderName ProviderOf(AcceptedInvitation? session)
    {
        var user = httpContextAccessor.HttpContext?.User;

        return (IdentityProviderName)identityProviderResolver.ResolveFrom(
        [
            user?.FindFirstValue(CanonicalProviderKeyClaim),
            user?.FindFirstValue(CanonicalIssuerClaim),
            user?.FindFirstValue("iss"),
            session?.IdentityProvider
        ]);
    }

    AcceptedInvitation? SessionFor(InvitationId invitationId, string? subject)
    {
        // The invitation id is persisted as a BSON UUID, and both LINQ translation and driver-side value
        // serialization of an EventSourceId-typed filter are unreliable against it - they silently match
        // nothing. Read the (small) accepted-invitation set and compare the deserialized values instead.
        var sessions = acceptedInvitations
            .Find(Builders<AcceptedInvitation>.Filter.Empty)
            .ToList()
            .Where(session => invitationId == InvitationId.NotSet || session.InvitationId.Value == invitationId.Value)
            .OrderByDescending(session => session.AcceptedAtUtc)
            .ToArray();

        // One invitation link can be opened by more than one login - a first attempt through one
        // provider, a second through another - and each authentication stores its own session. Only the
        // session belonging to the login making this request describes the person onboarding; the most
        // recent one is the best guess left when the request itself carries no subject.
        return sessions.FirstOrDefault(session => subject is not null && session.Subject == subject)
            ?? sessions.FirstOrDefault();
    }
}
