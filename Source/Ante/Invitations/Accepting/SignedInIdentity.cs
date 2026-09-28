// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Ante.IdentityProviders;
using Ante.Organization.Registration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
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
    /// Resolves the identity provider and compliance subject for the current request. Callers must reject an empty provider.
    /// </summary>
    /// <param name="invitationId">The invitation being accepted, or <see cref="InvitationId.NotSet"/> for self-service registration.</param>
    /// <param name="fallbackSubject">The subject to fall back to when the request itself carries none.</param>
    /// <returns>The resolved identity provider and compliance subject.</returns>
    (IdentityProviderName Provider, Cratis.Chronicle.Subject Subject) Resolve(InvitationId invitationId, Cratis.Chronicle.Subject fallbackSubject);

    /// <summary>
    /// Resolves the identity provider for the current request; an empty provider does not identify an owner.
    /// </summary>
    /// <returns>The resolved identity provider.</returns>
    IdentityProviderName ResolveProvider();

    /// <summary>
    /// Determines whether the current actor owns an invitation. In attested mode only an exact, live
    /// attested session grants access; in legacy mode the forwarded invitation jti or legacy session does.
    /// </summary>
    /// <remarks>
    /// This is the authorization gate every invitation-bound onboarding command must pass before acting:
    /// a known invitation id alone - visible in a URL, a token, or a shared link - must never be enough
    /// to act on somebody else's onboarding state. The invitation's subject may be a fallback for
    /// event attribution in <see cref="Resolve"/>, never proof of request ownership: a request that
    /// names no subject, or one whose subject was never recorded for this invitation, is rejected.
    /// </remarks>
    /// <param name="invitationId">The invitation to verify ownership of.</param>
    /// <returns>True when the current request verifiably owns the invitation; otherwise false.</returns>
    bool IsVerifiedOwnerOf(InvitationId invitationId);

    /// <summary>
    /// Resolves the current registration owner only from the forwarded sign-in identity, never from an invitation session.
    /// </summary>
    /// <returns>The owner, or null when the request has no subject.</returns>
    RegistrationOwner? CurrentRegistrationOwner();

    /// <summary>
    /// Checks whether the current login matches a durably recorded registration owner.
    /// </summary>
    /// <param name="owner">The owner recorded with the registration.</param>
    /// <returns>True only for the same subject and provider.</returns>
    bool IsVerifiedRegistrationOwner(RegistrationOwner owner);

    /// <summary>
    /// Resolves the invitation belonging to the current actor's live session (or forwarded jti in legacy mode).
    /// </summary>
    /// <returns>The invitation id, or <see cref="InvitationId.NotSet"/> when there is no verified session.</returns>
    InvitationId CurrentInvitationId();
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
/// <param name="exchangeConfig">The selected exchange trust mode.</param>
/// <param name="attestedSessions">The dedicated attested session store.</param>
public class SignedInIdentity(
    IHttpContextAccessor httpContextAccessor,
    IMongoCollection<AcceptedInvitation> acceptedInvitations,
    IIdentityProviderResolver identityProviderResolver,
    IOptions<InvitationExchangeConfig>? exchangeConfig = null,
    IMongoCollection<AttestedInvitationSession>? attestedSessions = null) : ISignedInIdentity
{
    bool IsAttested => exchangeConfig?.Value.Mode == InvitationExchangeMode.Attested;

    /// <inheritdoc/>
    public (IdentityProviderName Provider, Cratis.Chronicle.Subject Subject) Resolve(InvitationId invitationId, Cratis.Chronicle.Subject fallbackSubject)
    {
        if (IsAttested)
        {
            var attested = AttestedSessionFor(invitationId);
            return attested is null
                ? ((IdentityProviderName)string.Empty, fallbackSubject)
                : ((IdentityProviderName)attested.ProviderKey, (Cratis.Chronicle.Subject)attested.ProviderSubject);
        }

        var subject = SubjectOfCurrentRequest();
        var session = SessionFor(invitationId, subject, ProviderOf(null));

        return (ProviderOf(session), SubjectOf(subject, session, fallbackSubject));
    }

    /// <inheritdoc/>
    public IdentityProviderName ResolveProvider()
    {
        if (IsAttested)
        {
            return (IdentityProviderName)(AttestedSessionFor(InvitationId.NotSet)?.ProviderKey ?? string.Empty);
        }

        var subject = SubjectOfCurrentRequest();

        return ProviderOf(SessionFor(InvitationId.NotSet, subject, ProviderOf(null)));
    }

    /// <inheritdoc/>
    public bool IsVerifiedOwnerOf(InvitationId invitationId)
    {
        if (invitationId == InvitationId.NotSet)
        {
            return false;
        }

        // A forwarded jti is correlation, never authority, in attested mode.
        if (IsAttested)
        {
            return AttestedSessionFor(invitationId) is not null;
        }

        var jti = httpContextAccessor.HttpContext?.User?.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (Guid.TryParse(jti, out var jtiInvitationId) && (InvitationId)jtiInvitationId == invitationId)
        {
            return true;
        }

        var subject = SubjectOfCurrentRequest();

        // Ownership requires a subject that names this request, matched to a live session recorded
        // for this exact invitation. A request with no forwarded subject cannot select a session.
        var provider = ProviderOf(null);
        return subject is not null && !string.IsNullOrWhiteSpace(provider.Value) &&
            SessionFor(invitationId, subject, provider) is not null;
    }

    /// <inheritdoc/>
    public InvitationId CurrentInvitationId()
    {
        if (IsAttested)
        {
            var claimed = ForwardedInvitationId();
            return AttestedSessionFor(claimed)?.InvitationId ?? InvitationId.NotSet;
        }

        var jti = httpContextAccessor.HttpContext?.User?.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (Guid.TryParse(jti, out var invitationGuid))
        {
            return invitationGuid;
        }

        var subject = SubjectOfCurrentRequest();
        var provider = ProviderOf(null);
        return subject is null || string.IsNullOrWhiteSpace(provider.Value)
            ? InvitationId.NotSet
            : SessionFor(InvitationId.NotSet, subject, provider)?.InvitationId ?? InvitationId.NotSet;
    }

    /// <inheritdoc/>
    public RegistrationOwner? CurrentRegistrationOwner() => RegistrationOwner.Resolve(httpContextAccessor, identityProviderResolver);

    /// <inheritdoc/>
    public bool IsVerifiedRegistrationOwner(RegistrationOwner owner) =>
        CurrentRegistrationOwner() is { } current &&
        !string.IsNullOrWhiteSpace(current.Subject.Value) &&
        !string.IsNullOrWhiteSpace(current.Provider.Value) &&
        !string.IsNullOrWhiteSpace(owner.Provider.Value) &&
        current.Subject == owner.Subject && current.Provider == owner.Provider;

    /// <summary>
    /// Selects the session a request belongs to from candidate accepted-invitation sessions - the
    /// pure decision at the heart of <see cref="Resolve"/>, <see cref="ResolveProvider"/> and
    /// <see cref="IsVerifiedOwnerOf"/>, kept free of the Mongo round-trip so it can be exercised directly.
    /// </summary>
    /// <param name="allSessions">Candidate accepted-invitation sessions for the request subject.</param>
    /// <param name="invitationId">The invitation to select a session for, or <see cref="InvitationId.NotSet"/> for any invitation.</param>
    /// <param name="subject">The subject of the current request, or null when the request carries none.</param>
    /// <param name="provider">The provider resolved from the current request.</param>
    /// <param name="now">The current time, used to exclude expired sessions.</param>
    /// <returns>The selected session, or null when none qualifies.</returns>
    internal static AcceptedInvitation? SelectSession(IEnumerable<AcceptedInvitation> allSessions, InvitationId invitationId, string? subject, IdentityProviderName provider, DateTimeOffset now)
    {
        var sessions = allSessions
            .Where(session => session.ExpiresAtUtc > now
                && session.IdentityProvider == provider.Value
                && (invitationId == InvitationId.NotSet || session.InvitationId.Value == invitationId.Value))
            .OrderByDescending(session => session.AcceptedAtUtc)
            .ToArray();

        // One invitation link can be opened by more than one login. A request without a subject
        // cannot select one of those sessions; a different subject is contradictory evidence.
        return subject is null ? null : sessions.FirstOrDefault(session => session.Subject == subject);
    }

    internal static AttestedInvitationSession? SelectAttestedSession(
        IEnumerable<AttestedInvitationSession> sessions,
        string scope,
        InvitationId invitationId,
        string providerKey,
        string providerIssuer,
        string subject,
        DateTime now) =>
        sessions.Where(row => row.ExpiresAtUtc > now && row.LobbyScope == scope &&
            (invitationId == InvitationId.NotSet || row.InvitationId == invitationId) &&
            row.ProviderKey == providerKey && row.ProviderIssuer == providerIssuer && row.ProviderSubject == subject)
            .OrderByDescending(row => row.ExpiresAtUtc).FirstOrDefault();

    static Cratis.Chronicle.Subject SubjectOf(string? subject, AcceptedInvitation? session, Cratis.Chronicle.Subject fallbackSubject)
    {
        var resolved = subject ?? (string.IsNullOrWhiteSpace(session?.Subject) ? null : session.Subject);

        return resolved is null ? fallbackSubject : new Cratis.Chronicle.Subject(resolved);
    }

    InvitationId ForwardedInvitationId() =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User?.FindFirstValue(JwtRegisteredClaimNames.Jti), out var id)
            ? (InvitationId)id : InvitationId.NotSet;

    AttestedInvitationSession? AttestedSessionFor(InvitationId invitationId)
    {
        var claims = httpContextAccessor.HttpContext?.User?.Claims.Select(claim => new KeyValuePair<string, string>(claim.Type, claim.Value)).ToArray() ?? [];
        var report = AuthProxySignInReport.FromClaims(claims);
        var subject = claims.FirstOrDefault(claim => claim.Key == "urn:cratis:identity:subject").Value;
        var scope = exchangeConfig?.Value.Attestation.LobbyScope;
        if (attestedSessions is null || string.IsNullOrWhiteSpace(scope) || string.IsNullOrWhiteSpace(subject) ||
            string.IsNullOrWhiteSpace(report.ProviderKey) || string.IsNullOrWhiteSpace(report.Issuer))
        {
            return null;
        }

        // Query on primitive BSON fields; compare all authority-bearing values again after decoding.
        var sessions = attestedSessions.Find(Builders<AttestedInvitationSession>.Filter.Eq(row => row.ProviderSubject, subject)).ToList();
        return SelectAttestedSession(sessions, scope, invitationId, report.ProviderKey, report.Issuer, subject, DateTime.UtcNow);
    }

    string? SubjectOfCurrentRequest() => ForwardedIdentitySubject.Resolve(httpContextAccessor);

    IdentityProviderName ProviderOf(AcceptedInvitation? session) =>
        (IdentityProviderName)ForwardedIdentityProvider.Resolve(httpContextAccessor, identityProviderResolver, session?.IdentityProvider);

    AcceptedInvitation? SessionFor(InvitationId invitationId, string? subject, IdentityProviderName provider)
    {
        // A request without a forwarded subject cannot identify a login's exchange session.
        if (subject is null)
        {
            return null;
        }

        // The invitation id is persisted as a BSON UUID; comparing its EventSourceId wrapper in a
        // driver-side filter can silently miss it. Filter by the primitive subject, then compare
        // invitation id, provider and expiry against the deserialized candidate sessions.
        var allSessions = acceptedInvitations.Find(Builders<AcceptedInvitation>.Filter.Eq(a => a.Subject, subject)).ToList();

        return SelectSession(allSessions, invitationId, subject, provider, DateTimeOffset.UtcNow);
    }
}
