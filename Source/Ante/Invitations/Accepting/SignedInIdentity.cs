// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Ante.IdentityProviders;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
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

    /// <summary>
    /// Determines whether the current request is a verified owner of an invitation - either the invite
    /// token's own <c>jti</c> claim names it directly, or the request's subject has exchanged this exact
    /// invitation for a session that has not yet expired.
    /// </summary>
    /// <remarks>
    /// This is the authorization gate every invitation-bound onboarding command must pass before acting:
    /// a known invitation id alone - visible in a URL, a token, or a shared link - must never be enough
    /// to act on somebody else's onboarding state. There is deliberately no "best guess" fallback here
    /// the way there is in <see cref="Resolve"/> - a request that names no subject, or one whose subject
    /// was never recorded against this exact invitation, proves nothing and is rejected outright.
    /// </remarks>
    /// <param name="invitationId">The invitation to verify ownership of.</param>
    /// <returns>True when the current request verifiably owns the invitation; otherwise false.</returns>
    bool IsVerifiedOwnerOf(InvitationId invitationId);
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

    /// <inheritdoc/>
    public bool IsVerifiedOwnerOf(InvitationId invitationId)
    {
        if (invitationId == InvitationId.NotSet)
        {
            return false;
        }

        // The invite token's own jti claim, forwarded directly by the authentication proxy's invite
        // claims enricher, names the invitation outright - it comes from the token Ante itself issued
        // rather than from a session record correlated after the fact, so it is trusted on its own.
        var jti = httpContextAccessor.HttpContext?.User?.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (Guid.TryParse(jti, out var jtiInvitationId) && (InvitationId)jtiInvitationId == invitationId)
        {
            return true;
        }

        var subject = SubjectOfCurrentRequest();

        // Deliberately not SessionFor's "no subject on the request" fallback - that heuristic exists to
        // pick which login a request belongs to when nothing else disambiguates it, never to decide
        // whether it is authorized to act on someone else's invitation. Ownership requires a subject that
        // names this request, matched to a live session recorded for this exact invitation.
        return subject is not null && SessionFor(invitationId, subject) is not null;
    }

    /// <summary>
    /// Selects the session a request belongs to out of every recorded accepted-invitation session - the
    /// pure decision at the heart of <see cref="Resolve"/>, <see cref="ResolveProvider"/> and
    /// <see cref="IsVerifiedOwnerOf"/>, kept free of the Mongo round-trip so it can be exercised directly.
    /// </summary>
    /// <param name="allSessions">Every recorded accepted-invitation session.</param>
    /// <param name="invitationId">The invitation to select a session for, or <see cref="InvitationId.NotSet"/> for any invitation.</param>
    /// <param name="subject">The subject of the current request, or null when the request carries none.</param>
    /// <param name="now">The current time, used to exclude expired sessions.</param>
    /// <returns>The selected session, or null when none qualifies.</returns>
    internal static AcceptedInvitation? SelectSession(IEnumerable<AcceptedInvitation> allSessions, InvitationId invitationId, string? subject, DateTimeOffset now)
    {
        var sessions = allSessions
            .Where(session => session.ExpiresAtUtc > now
                && (invitationId == InvitationId.NotSet || session.InvitationId.Value == invitationId.Value))
            .OrderByDescending(session => session.AcceptedAtUtc)
            .ToArray();

        // One invitation link can be opened by more than one login - a first attempt through one
        // provider, a second through another - and each authentication stores its own session. Only the
        // session belonging to the login making this request describes the person onboarding. When the
        // request names a subject, a mismatch is contradictory evidence, not missing evidence - the
        // request has already said who it is, and no other session is a substitute for that answer. The
        // most recent session is only ever the best guess left when the request itself carries no
        // subject to disambiguate with.
        return subject is null
            ? sessions.FirstOrDefault()
            : sessions.FirstOrDefault(session => session.Subject == subject);
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
        var allSessions = acceptedInvitations.Find(Builders<AcceptedInvitation>.Filter.Empty).ToList();

        return SelectSession(allSessions, invitationId, subject, DateTimeOffset.UtcNow);
    }
}
