// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Ante.IdentityProviders;
using Ante.Invitations.Issuing;
using Cratis.Arc.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting;

/// <summary>
/// The request body sent by the host's authentication proxy to the invite-exchange endpoint after the
/// user completes OIDC login.
/// </summary>
/// <param name="Subject">The subject (<c>sub</c>) claim of the authenticated user.</param>
/// <param name="IdentityProvider">
/// What the authentication proxy can say about the provider the user authenticated with - its
/// <c>iss</c> claim, or nothing at all for an OAuth2-only provider, which issues none. It is resolved
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
/// <param name="Subject">The subject (<c>sub</c>) claim of the authenticated user.</param>
/// <param name="IdentityProvider">The resolved identity provider the user authenticated with.</param>
/// <param name="InvitationId">The invitation exchanged.</param>
/// <param name="FlowType">The type of invitation flow this login is going through.</param>
/// <param name="AcceptedAtUtc">When the exchange happened.</param>
public record AcceptedInvitation(
    string Subject,
    string IdentityProvider,
    InvitationId InvitationId,
    InvitationFlowType FlowType,
    DateTimeOffset AcceptedAtUtc);

/// <summary>
/// Validates and records an invite-exchange request, shared between the bypass middleware and the
/// fallback controller.
/// </summary>
public static class InviteExchangeProcessor
{
    /// <summary>
    /// Validates the bearer token carried on the exchange request and, when valid, records the session.
    /// </summary>
    /// <param name="authorizationHeader">The <c>Authorization</c> header of the exchange request.</param>
    /// <param name="request">The exchange request body.</param>
    /// <param name="acceptedInvitations">The collection accepted invitation sessions are recorded in.</param>
    /// <param name="identityProviderResolver">Resolver used to normalize the reported identity provider.</param>
    /// <returns>True when the token was valid and the session was recorded.</returns>
    public static async Task<bool> TryStoreAcceptedInvitation(
        string authorizationHeader,
        ExchangeInviteRequest request,
        IMongoCollection<AcceptedInvitation> acceptedInvitations,
        IIdentityProviderResolver identityProviderResolver)
    {
        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var token = authorizationHeader["Bearer ".Length..].Trim();

        InvitationId invitationId;
        InvitationFlowType flowType;

        try
        {
            var handler = new JsonWebTokenHandler();
            var jwt = handler.ReadJsonWebToken(token);
            if (!Guid.TryParse(jwt.Id, out var guid))
            {
                return false;
            }

            invitationId = guid;
            var inviteTypeClaimValue = jwt.Claims
                .FirstOrDefault(claim => claim.Type == InvitationClaims.InvitationType)
                ?.Value;
            flowType = Enum.TryParse<InvitationFlowType>(inviteTypeClaimValue, out var parsedInviteType)
                ? parsedInviteType
                : InvitationFlowType.JoinTenant;
        }
        catch (Exception)
        {
            return false;
        }

        // Resolved on the way in, so the session records the provider the user actually authenticated
        // with rather than a placeholder that has to be un-guessed everywhere it is later read.
        var normalizedIdentityProvider = identityProviderResolver.ResolveFrom([request.ProviderKey, request.Issuer, request.IdentityProvider]);
        var acceptedInvitation = new AcceptedInvitation(
            request.Subject,
            normalizedIdentityProvider,
            invitationId,
            flowType,
            DateTimeOffset.UtcNow);

        await acceptedInvitations.ReplaceOneAsync(
            a => a.Subject == request.Subject && a.IdentityProvider == normalizedIdentityProvider,
            acceptedInvitation,
            new ReplaceOptions { IsUpsert = true });

        return true;
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
    public async Task InvokeAsync(
        HttpContext context,
        IMongoCollection<AcceptedInvitation> acceptedInvitations,
        IIdentityProviderResolver identityProviderResolver)
    {
        if (HttpMethods.IsPost(context.Request.Method)
            && context.Request.Path.Equals("/_invite/exchange", StringComparison.OrdinalIgnoreCase))
        {
            ExchangeInviteRequest? request;
            try
            {
                request = await context.Request.ReadFromJsonAsync<ExchangeInviteRequest>();
            }
            catch (Exception)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            if (request is null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var success = await InviteExchangeProcessor.TryStoreAcceptedInvitation(
                context.Request.Headers.Authorization.ToString(),
                request,
                acceptedInvitations,
                identityProviderResolver);

            context.Response.StatusCode = success
                ? StatusCodes.Status200OK
                : StatusCodes.Status400BadRequest;
            return;
        }

        await next(context);
    }
}

/// <summary>
/// Fallback controller-based invite-exchange endpoint, reached when the bypass middleware is not in the
/// pipeline (for example in specs that exercise the controller directly).
/// </summary>
/// <param name="acceptedInvitations">The collection accepted invitation sessions are recorded in.</param>
/// <param name="identityProviderResolver">Resolver used to normalize the reported identity provider.</param>
[Route("_invite/exchange")]
[ApiController]
public class InviteExchangeController(
    IMongoCollection<AcceptedInvitation> acceptedInvitations,
    IIdentityProviderResolver identityProviderResolver) : ControllerBase
{
    /// <summary>
    /// Exchanges an invitation token for a recorded acceptance session.
    /// </summary>
    /// <param name="request">The exchange request.</param>
    /// <returns>200 when the exchange succeeded; otherwise 400.</returns>
    [HttpPost]
    public async Task<IActionResult> Exchange([FromBody] ExchangeInviteRequest request)
    {
        var success = await InviteExchangeProcessor.TryStoreAcceptedInvitation(
            Request.Headers.Authorization.ToString(),
            request,
            acceptedInvitations,
            identityProviderResolver);

        return success ? Ok() : BadRequest();
    }
}

/// <summary>
/// Application-level identity details for an invited user going through Ante.
/// </summary>
/// <param name="InvitationId">The invitation identifier associated with this user session.</param>
/// <param name="FlowType">The type of invitation flow this user is going through.</param>
public record InvitationIdentityDetails(InvitationId InvitationId, InvitationFlowType FlowType);

/// <summary>
/// Provides identity details for the Ante application.
/// A user is only authorized when the authentication proxy has forwarded a valid <c>jti</c> claim from
/// the invite token, and that claim corresponds to a pending invitation.
/// The <c>jti</c> and <c>invite_type</c> claims are forwarded by the authentication proxy's invite
/// claims enricher.
/// </summary>
/// <param name="acceptedInvitations">Collection used to resolve accepted invitation sessions for fallback identity resolution.</param>
public class InvitationIdentityProvider(
    IMongoCollection<AcceptedInvitation> acceptedInvitations) : IProvideIdentityDetails<InvitationIdentityDetails>
{
    /// <inheritdoc/>
    public async Task<IdentityDetails> Provide(IdentityProviderContext context)
    {
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

        var subject = context.Claims
            .FirstOrDefault(c => c.Key == ClaimTypes.NameIdentifier).Value
            ?? context.Claims.FirstOrDefault(c => c.Key == "sub").Value;

        if (string.IsNullOrWhiteSpace(subject))
        {
            return new IdentityDetails(true, new InvitationIdentityDetails(InvitationId.NotSet, InvitationFlowType.JoinTenant));
        }

        // The subject alone identifies the login, so the most recent session it authenticated is the
        // one this request belongs to. Narrowing by provider as well only ever risked missing the
        // session when the two sides had attributed the same sign-in differently.
        var acceptedInvitation = await acceptedInvitations
            .Find(a => a.Subject == subject)
            .SortByDescending(a => a.AcceptedAtUtc)
            .FirstOrDefaultAsync();

        return acceptedInvitation is null
            ? new IdentityDetails(true, new InvitationIdentityDetails(InvitationId.NotSet, InvitationFlowType.JoinTenant))
            : new IdentityDetails(true, new InvitationIdentityDetails(acceptedInvitation.InvitationId, acceptedInvitation.FlowType));
    }
}
