// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Ante.IdentityProviders;
using Ante.Invitations.Issuing;
using Cratis.Arc.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting;

/// <summary>Outcome of the shared invite-exchange write boundary.</summary>
public enum InviteExchangeOutcome
{
    /// <summary>The token was valid and the session was recorded.</summary>
    Accepted,

    /// <summary>The request was refused: it is malformed, unverifiable or names an unresolvable provider.</summary>
    Rejected,

    /// <summary>The exchange storage is not ready, so nothing was read or written; the caller may retry.</summary>
    Unavailable,
}

/// <summary>
/// The request body sent by the host's authentication proxy to the invite-exchange endpoint after the
/// user completes OIDC login.
/// </summary>
/// <param name="Subject">The subject (<c language="csharp">sub</c>) claim of the authenticated user.</param>
/// <param name="IdentityProvider">
/// What the authentication proxy can say about the provider the user authenticated with - its
/// <c language="csharp">iss</c> claim, or nothing at all for an OAuth2-only provider, which issues none. It is resolved
/// against the deployment's configured providers rather than recorded verbatim.
/// </param>
/// <param name="ProviderKey">
/// The canonical provider key, sent only when the authentication proxy is configured to resolve a
/// canonical federated identity for the provider. It is the one value that names a federated provider
/// outright, so it is preferred over everything else when present.
/// </param>
/// <param name="Issuer">The normalized issuer of the canonical federated identity, sent alongside the key.</param>
public record ExchangeInviteRequest(string Subject, string IdentityProvider, string? ProviderKey, string? Issuer);

/// <summary>
/// The session recorded when a login exchanges an invitation token, so later requests from the same
/// login can be traced back to the invitation without the token itself.
/// </summary>
/// <param name="Subject">The subject (<c language="csharp">sub</c>) claim of the authenticated user.</param>
/// <param name="IdentityProvider">The resolved identity provider the user authenticated with.</param>
/// <param name="InvitationId">The invitation exchanged.</param>
/// <param name="FlowType">The type of invitation flow this login is going through.</param>
/// <param name="AcceptedAtUtc">When the exchange happened.</param>
/// <param name="ExpiresAtUtc">
/// When this session stops authorizing, copied verbatim from the invitation token's own <c language="csharp">exp</c>
/// claim. Because it is never computed from "now", retrying the exchange with the same token can never
/// push it further out - it is the same value every time.
/// </param>
public record AcceptedInvitation(
    string Subject,
    string IdentityProvider,
    InvitationId InvitationId,
    InvitationFlowType FlowType,
    DateTimeOffset AcceptedAtUtc,
    DateTimeOffset ExpiresAtUtc);

/// <summary>
/// Validates and records an invite-exchange request, shared between the bypass middleware and the
/// fallback controller.
/// </summary>
public static class InviteExchangeProcessor
{
    internal const string InvitationIdField = "invitationId";

    /// <summary>
    /// Validates the bearer token carried on the exchange request and, when valid, records the session.
    /// </summary>
    /// <param name="authorizationHeader">The <c language="csharp">Authorization</c> header of the exchange request.</param>
    /// <param name="request">The exchange request body.</param>
    /// <param name="acceptedInvitations">The collection accepted invitation sessions are recorded in.</param>
    /// <param name="identityProviderResolver">Resolver used to normalize the reported identity provider.</param>
    /// <param name="tokenValidator">Verifier of invitation signatures and claims.</param>
    /// <param name="logger">Logger for rejected unresolved provider evidence.</param>
    /// <param name="indexes">Readiness of the exchange storage; nothing is validated or written before it is ready.</param>
    /// <returns>Whether the session was recorded, the request was rejected, or the storage was not ready.</returns>
    public static async Task<InviteExchangeOutcome> TryStoreAcceptedInvitation(
        string authorizationHeader,
        ExchangeInviteRequest request,
        IMongoCollection<AcceptedInvitation> acceptedInvitations,
        IIdentityProviderResolver identityProviderResolver,
        IInvitationTokenValidator tokenValidator,
        ILogger<InviteExchangeBypassMiddleware> logger,
        IExchangeIndexReadiness indexes)
    {
        if (!indexes.IsReady)
        {
            return InviteExchangeOutcome.Unavailable;
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            return InviteExchangeOutcome.Rejected;
        }

        var verifiedToken = await tokenValidator.Validate(authorizationHeader);
        if (verifiedToken is null)
        {
            return InviteExchangeOutcome.Rejected;
        }

        // Resolved on the way in, so the session records the provider the user actually authenticated
        // with rather than a placeholder that has to be un-guessed everywhere it is later read.
        var normalizedIdentityProvider = identityProviderResolver.ResolveFrom(
            new AuthProxySignInReport(request.ProviderKey, request.Issuer, request.IdentityProvider).Candidates());
        if (string.IsNullOrWhiteSpace(normalizedIdentityProvider) ||
            normalizedIdentityProvider.Equals(IdentityProviderResolver.Unidentified, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogUnresolvedExchangeProvider();
            return InviteExchangeOutcome.Rejected;
        }

        var acceptedInvitation = new AcceptedInvitation(
            request.Subject,
            normalizedIdentityProvider,
            verifiedToken.InvitationId,
            verifiedToken.FlowType,
            DateTimeOffset.UtcNow,
            verifiedToken.ExpiresAtUtc);

        // A single upsert is the whole idempotency story here: at-least-once delivery from the
        // authentication proxy, a user double-submitting, or a lost response all replay the very same
        // token, which always resolves to the same invitation id and the same expiry (copied from the
        // token's own exp claim, never computed from "now"). A retry therefore replaces the session with
        // an equivalent one instead of creating a duplicate or extending its lifetime.
        //
        // The session belongs to the invitation as well as to the login. One person is routinely holding
        // several invitations to the same organization - a resend, a refresh and a revocation each mint a
        // new invitation id - and a login that opens a second link must not take over the session of the
        // first: that made the first, still-pending invitation fail its owner check with "no longer
        // pending" (Cratis/StudioIssues#581, #582).
        await acceptedInvitations.ReplaceOneAsync(
            SessionFilter(request.Subject, normalizedIdentityProvider, verifiedToken.InvitationId),
            acceptedInvitation,
            new ReplaceOptions { IsUpsert = true });

        return InviteExchangeOutcome.Accepted;
    }

    /// <summary>
    /// The filter identifying the one session a login holds for one invitation.
    /// </summary>
    /// <param name="subject">The subject of the authenticated user.</param>
    /// <param name="identityProvider">The resolved identity provider the user authenticated with.</param>
    /// <param name="invitationId">The invitation the session belongs to.</param>
    /// <returns>The filter.</returns>
    /// <remarks>
    /// The invitation id is matched on its stored form - a standard BSON UUID - rather than through its
    /// <see cref="EventSourceId{T}"/> wrapper, which a driver-side filter can silently fail to match.
    /// </remarks>
    internal static FilterDefinition<AcceptedInvitation> SessionFilter(string subject, string identityProvider, InvitationId invitationId) =>
        Builders<AcceptedInvitation>.Filter.And(
            Builders<AcceptedInvitation>.Filter.Eq(a => a.Subject, subject),
            Builders<AcceptedInvitation>.Filter.Eq(a => a.IdentityProvider, identityProvider),
            Builders<AcceptedInvitation>.Filter.Eq(InvitationIdField, new BsonBinaryData(invitationId.Value, GuidRepresentation.Standard)));
}

/// <summary>
/// Installs the MongoDB indexes <see cref="AcceptedInvitation"/> needs before the exchange endpoint accepts
/// writes; <see cref="AcceptedInvitationIndexRegistration"/> runs it in the background and reports readiness.
/// </summary>
public static class AcceptedInvitationIndexes
{
    const string UniqueSessionIndexName = "UniqueAcceptedInvitationSessionPerInvitation";
    const string LegacyUniqueSessionIndexName = "UniqueAcceptedInvitationSession";
    const int IndexNotFoundCode = 27;
    const int NamespaceNotFoundCode = 26;
    static readonly FieldDefinition<AcceptedInvitation> _invitationIdentifierField = InviteExchangeProcessor.InvitationIdField;

    /// <summary>
    /// Creates the indexes, if they do not already exist. Safe to call every time the application
    /// starts - <c language="csharp">CreateManyAsync</c> is a no-op for an index that already matches.
    /// </summary>
    /// <param name="acceptedInvitations">The collection to create indexes on.</param>
    /// <param name="cancellationToken">Cancels a pending connection on shutdown.</param>
    public static Task EnsureCreated(IMongoCollection<AcceptedInvitation> acceptedInvitations, CancellationToken cancellationToken = default)
    {
        // Backstops the single-document-per-login-and-invitation invariant the exchange's upsert relies on; the
        // upsert itself is already atomic, so this is defense in depth rather than the source of that guarantee.
        var uniqueSession = new CreateIndexModel<AcceptedInvitation>(
            Builders<AcceptedInvitation>.IndexKeys
                .Ascending(a => a.Subject)
                .Ascending(a => a.IdentityProvider)
                .Ascending(_invitationIdentifierField),
            new CreateIndexOptions { Unique = true, Name = UniqueSessionIndexName });

        // Storage cleanup only - not the authorization-time expiry check. MongoDB only sweeps expired
        // documents periodically, but InvitationIdentityProvider must stop honoring a session the instant
        // it expires, so it checks ExpiresAtUtc itself rather than relying on this index having run yet.
        var expiryCleanup = new CreateIndexModel<AcceptedInvitation>(
            Builders<AcceptedInvitation>.IndexKeys.Ascending(a => a.ExpiresAtUtc),
            new CreateIndexOptions { ExpireAfter = TimeSpan.Zero, Name = "AcceptedInvitationExpiry" });

        return CreateWithoutTheLegacyIndex(acceptedInvitations, [uniqueSession, expiryCleanup], cancellationToken);
    }

    // The index this replaces allowed a login a single session, so a second invitation exchanged by the same
    // person overwrote the first. It has to go before sessions per invitation can be stored, and it is dropped
    // on every start-up that finds it - dropping what is not there is not an error.
    static async Task CreateWithoutTheLegacyIndex(
        IMongoCollection<AcceptedInvitation> acceptedInvitations,
        IEnumerable<CreateIndexModel<AcceptedInvitation>> indexes,
        CancellationToken cancellationToken)
    {
        try
        {
            await acceptedInvitations.Indexes.DropOneAsync(LegacyUniqueSessionIndexName, cancellationToken);
        }
        catch (MongoCommandException ex) when (ex.Code is IndexNotFoundCode or NamespaceNotFoundCode)
        {
        }

        await acceptedInvitations.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }
}

/// <summary>
/// Handles the invite-exchange request inline, ahead of the rest of the pipeline, so it works even
/// before authorization has resolved an identity for the request.
/// </summary>
/// <param name="next">The next middleware in the pipeline.</param>
public class InviteExchangeBypassMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="acceptedInvitations">The collection accepted invitation sessions are recorded in.</param>
    /// <param name="identityProviderResolver">Resolver used to normalize the reported identity provider.</param>
    /// <param name="tokenValidator">The invitation token verifier.</param>
    /// <param name="logger">Logger for rejected unresolved provider evidence.</param>
    /// <param name="indexes">Shared readiness of the exchange storage.</param>
    public async Task InvokeAsync(
        HttpContext context,
        IMongoCollection<AcceptedInvitation> acceptedInvitations,
        IIdentityProviderResolver identityProviderResolver,
        IInvitationTokenValidator tokenValidator,
        ILogger<InviteExchangeBypassMiddleware> logger,
        IExchangeIndexReadiness indexes)
    {
        if (HttpMethods.IsPost(context.Request.Method)
            && context.Request.Path.Equals("/_invite/exchange", StringComparison.OrdinalIgnoreCase))
        {
            var request = await ReadRequest(context);
            if (request is null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var outcome = await InviteExchangeProcessor.TryStoreAcceptedInvitation(
                context.Request.Headers.Authorization.ToString(),
                request,
                acceptedInvitations,
                identityProviderResolver,
                tokenValidator,
                logger,
                indexes);

            context.Response.StatusCode = outcome switch
            {
                InviteExchangeOutcome.Accepted => StatusCodes.Status200OK,
                InviteExchangeOutcome.Unavailable => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status400BadRequest,
            };
            return;
        }

        await next(context);
    }

    static async Task<ExchangeInviteRequest?> ReadRequest(HttpContext context)
    {
        try
        {
            return await context.Request.ReadFromJsonAsync<ExchangeInviteRequest>();
        }
        catch (Exception)
        {
            return null;
        }
    }
}

/// <summary>
/// Handles only attested invitation routes on the dedicated pipeline branch. Chronicle-dependent
/// collaborators are not resolved for liveness, readiness, or other application routes.
/// </summary>
/// <param name="next">The next middleware in the branch.</param>
public class AttestedInviteExchangeMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Handles a stage or completion request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="staging">The attested invitation staging service.</param>
    /// <param name="completion">The attested invitation completion service.</param>
    /// <param name="indexes">Shared readiness of every index either attested write depends on.</param>
    public async Task InvokeAsync(HttpContext context, AttestedInvitationStaging staging, AttestedInvitationCompletion completion, IExchangeIndexReadiness indexes)
    {
        if (!indexes.IsReady)
        {
            // Both attested routes write; refuse before the body is read or anything is staged.
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }

        if (context.Request.Path.Equals("/_invite/stage", StringComparison.OrdinalIgnoreCase))
        {
            await Stage(context, staging);
            return;
        }

        if (context.Request.Path.Equals("/_invite/exchange", StringComparison.OrdinalIgnoreCase))
        {
            // Never pass an attested request through the legacy processor, even during rollout.
            await Complete(context, completion);
            return;
        }

        await next(context);
    }

    static async Task Stage(HttpContext context, AttestedInvitationStaging staging)
    {
        var stage = await InvitationStageRequestBody.Read(context);
        if (HasBody(context, stage))
        {
            Respond(context, await staging.TryStage(context.Request.Headers.Authorization.ToString(), stage), StatusCodes.Status404NotFound);
        }
    }

    static async Task Complete(HttpContext context, AttestedInvitationCompletion completion)
    {
        var request = await InvitationCompletionRequestBody.Read(context);
        if (HasBody(context, request))
        {
            Respond(context, await completion.TryComplete(context.Request.Headers.Authorization.ToString(), request), StatusCodes.Status400BadRequest);
        }
    }

    static bool HasBody(HttpContext context, [NotNullWhen(true)] object? body)
    {
        if (body is not null)
        {
            return true;
        }

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return false;
    }

    static void Respond(HttpContext context, bool succeeded, int failureStatus) =>
        context.Response.StatusCode = succeeded ? StatusCodes.Status200OK : failureStatus;
}

/// <summary>
/// Fallback controller-based invite-exchange endpoint, reached when the bypass middleware is not in the
/// pipeline (for example in specs that exercise the controller directly).
/// </summary>
/// <param name="acceptedInvitations">The collection accepted invitation sessions are recorded in.</param>
/// <param name="identityProviderResolver">Resolver used to normalize the reported identity provider.</param>
/// <param name="tokenValidator">The invitation token verifier.</param>
/// <param name="logger">Logger for rejected unresolved provider evidence.</param>
/// <param name="indexes">Shared readiness of the exchange storage.</param>
/// <param name="exchangeConfig">The selected exchange protocol.</param>
[Route("_invite/exchange")]
[ApiController]
public class InviteExchangeController(
    IMongoCollection<AcceptedInvitation> acceptedInvitations,
    IIdentityProviderResolver identityProviderResolver,
    IInvitationTokenValidator tokenValidator,
    ILogger<InviteExchangeBypassMiddleware> logger,
    IExchangeIndexReadiness indexes,
    IOptions<InvitationExchangeConfig>? exchangeConfig = null) : ControllerBase
{
    /// <summary>
    /// Exchanges an invitation token for a recorded acceptance session.
    /// </summary>
    /// <param name="request">The exchange request.</param>
    /// <returns>200 when the exchange succeeded, 503 while the exchange storage is not ready; otherwise 400.</returns>
    [HttpPost]
    public async Task<IActionResult> Exchange([FromBody] ExchangeInviteRequest? request)
    {
        if (exchangeConfig?.Value.Mode == InvitationExchangeMode.Attested)
        {
            return BadRequest();
        }

        if (request is null)
        {
            return BadRequest();
        }

        var outcome = await InviteExchangeProcessor.TryStoreAcceptedInvitation(
            Request.Headers.Authorization.ToString(),
            request,
            acceptedInvitations,
            identityProviderResolver,
            tokenValidator,
            logger,
            indexes);

        return outcome switch
        {
            InviteExchangeOutcome.Accepted => Ok(),
            InviteExchangeOutcome.Unavailable => StatusCode(StatusCodes.Status503ServiceUnavailable),
            _ => BadRequest(),
        };
    }
}

/// <summary>
/// Application-level identity details for an invited user going through Ante.
/// </summary>
/// <param name="InvitationId">The invitation identifier associated with this user session.</param>
/// <param name="FlowType">The type of invitation flow this user is going through.</param>
public record InvitationIdentityDetails(InvitationId InvitationId, InvitationFlowType FlowType);

/// <summary>
/// Provides identity details for the Ante application. The proxy may forward <c language="csharp">jti</c>
/// and <c language="csharp">invite_type</c> claims from an invitation, or a live exchange session may
/// identify the invitation by the request's forwarded subject and resolved provider. This provider
/// supplies details; invitation-bound commands enforce ownership separately.
/// </summary>
/// <param name="acceptedInvitations">Collection used to resolve accepted invitation sessions for fallback identity resolution.</param>
/// <param name="identityProviderResolver">Resolver for the forwarded request's identity provider.</param>
/// <param name="exchangeConfig">The exchange mode.</param>
/// <param name="attestedSessions">The attested session collection.</param>
public class InvitationIdentityProvider(
    IMongoCollection<AcceptedInvitation> acceptedInvitations,
    IIdentityProviderResolver identityProviderResolver,
    IOptions<InvitationExchangeConfig>? exchangeConfig = null,
    IMongoCollection<AttestedInvitationSession>? attestedSessions = null) : IProvideIdentityDetails<InvitationIdentityDetails>
{
    /// <inheritdoc/>
    public async Task<IdentityDetails> Provide(IdentityProviderContext context)
    {
        if (exchangeConfig?.Value.Mode == InvitationExchangeMode.Attested)
        {
            return await ProvideAttested(context);
        }

        var jtiValue = context.Claims
            .FirstOrDefault(c => c.Key == JwtRegisteredClaimNames.Jti).Value;

        var inviteTypeValue = context.Claims
            .FirstOrDefault(c => c.Key == InvitationClaims.InvitationType).Value;

        if (Guid.TryParse(jtiValue, out var invitationGuid))
        {
            var flowType = Enum.TryParse<InvitationFlowType>(inviteTypeValue, out var parsedType)
                ? parsedType
                : InvitationFlowType.JoinTenant;

            return new IdentityDetails(true, new InvitationIdentityDetails(invitationGuid, flowType));
        }

        return await ProvideAcceptedSession(context);
    }

    static bool HasCanonicalProvider(AuthProxySignInReport report) =>
        !string.IsNullOrWhiteSpace(report.ProviderKey) && !string.IsNullOrWhiteSpace(report.Issuer);

    async Task<IdentityDetails> ProvideAttested(IdentityProviderContext context)
    {
        var report = AuthProxySignInReport.FromClaims(context.Claims);
        var attestedSubject = context.Claims.FirstOrDefault(claim => claim.Key == "urn:cratis:identity:subject").Value;
        var scope = exchangeConfig!.Value.Attestation.LobbyScope;
        if (!CanReadAttestedSessions(scope, attestedSubject) || !HasCanonicalProvider(report))
        {
            return new IdentityDetails(true, new InvitationIdentityDetails(InvitationId.NotSet, InvitationFlowType.JoinTenant));
        }

        var candidates = await attestedSessions!.Find(Builders<AttestedInvitationSession>.Filter.Eq(row => row.ProviderSubject, attestedSubject)).ToListAsync();
        var claimed = context.Claims.FirstOrDefault(claim => claim.Key == JwtRegisteredClaimNames.Jti).Value;
        var id = Guid.TryParse(claimed, out var guid) ? (InvitationId)guid : InvitationId.NotSet;
        var session = SignedInIdentity.SelectAttestedSession(candidates, scope, id, report.ProviderKey!, report.Issuer!, attestedSubject, DateTime.UtcNow);
        return session is null
            ? new IdentityDetails(true, new InvitationIdentityDetails(InvitationId.NotSet, InvitationFlowType.JoinTenant))
            : new IdentityDetails(true, new InvitationIdentityDetails(session.InvitationId, session.FlowType));
    }

    bool CanReadAttestedSessions(string scope, string? subject) =>
        attestedSessions is not null && !string.IsNullOrWhiteSpace(scope) && !string.IsNullOrWhiteSpace(subject);

    async Task<IdentityDetails> ProvideAcceptedSession(IdentityProviderContext context)
    {
        var subject = ForwardedIdentitySubject.Resolve(context.Claims, context.Id.Value);
        var provider = ForwardedIdentityProvider.Resolve(context.Claims, identityProviderResolver);
        if (subject is null || string.IsNullOrWhiteSpace(provider))
        {
            return new IdentityDetails(true, new InvitationIdentityDetails(InvitationId.NotSet, InvitationFlowType.JoinTenant));
        }

        // A subject can occur at more than one provider. Select only this request's live exchange
        // session, using the same rule as invitation queries and commands. The forwarded-jti path above
        // still relies on the proxy to have verified that claim independently.
        var sessions = await acceptedInvitations.Find(Builders<AcceptedInvitation>.Filter.Eq(a => a.Subject, subject)).ToListAsync();
        var acceptedInvitation = SignedInIdentity.SelectSession(sessions, InvitationId.NotSet, subject, (IdentityProviderName)provider, DateTimeOffset.UtcNow);

        return acceptedInvitation is null
            ? new IdentityDetails(true, new InvitationIdentityDetails(InvitationId.NotSet, InvitationFlowType.JoinTenant))
            : new IdentityDetails(true, new InvitationIdentityDetails(acceptedInvitation.InvitationId, acceptedInvitation.FlowType));
    }
}
