// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Invitations.Accepting;
using Ante.Invitations.Issuing;
using Cratis.Chronicle.Reactors;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.Receiving;

/// <summary>
/// Mints a signed token for every invitation Ante receives, and forwards it to the outbox so the host
/// can build the link it emails to the invitee.
/// </summary>
/// <remarks>
/// <para>
/// Runs against Ante's own store - the events it reacts to were just appended locally by
/// <see cref="IncomingInvitationSubscriptions"/>, so no cross-store subscription is needed here.
/// </para>
/// <para>
/// Every receipt and reissue request concludes with exactly one outcome in the outbox: a token or a rejection
/// (see <see cref="InvitationTokenOutbox"/>). Without a signing key the invitation is not failed - that would pause
/// its partition and, with quarantine thresholds configured, take the whole observer out of service - but recorded as
/// <see cref="InvitationTokenIssuanceDeferred"/>, and <see cref="InvitationTokenIssuanceResumption"/> resumes it once
/// an instance with a key runs.
/// </para>
/// <para>
/// Replay deliberately runs issuance again, so a lost publication can be recovered; the outbox check keeps it from
/// publishing an outcome twice. The live handlers are <see cref="OnceOnlyAttribute"/> only because they record the
/// deferral: a replay never records it again.
/// </para>
/// </remarks>
/// <param name="tokenIssuer">The canonical invitation token issuer.</param>
/// <param name="eventStore">The event store.</param>
/// <param name="logger">The warning logger for invalid invitation ids and deferred issuance.</param>
/// <param name="exchange">The selected exchange mode.</param>
/// <param name="token">The signing configuration; when absent, a key is assumed to be configured.</param>
[Reactor]
public class InvitationTokenIssuingReactor(
    IInvitationTokenIssuer tokenIssuer,
    IEventStore eventStore,
    ILogger<InvitationTokenIssuingReactor> logger,
    IOptions<InvitationExchangeConfig>? exchange = null,
    IOptions<InvitationTokenConfig>? token = null) : IReactor
{
    readonly InvitationTokenIssuance _issuance = new(tokenIssuer, eventStore, logger, exchange, token);

    /// <summary>
    /// Issues a join-tenant token and forwards it to the outbox, or defers it while no signing key is configured.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    /// <returns>The deferral, when no signing key is configured.</returns>
    [OnceOnly]
    public Task<InvitationTokenIssuanceDeferred?> On(JoinTenantInvitationReceived @event, EventContext context) =>
        _issuance.Receive(@event, context);

    /// <summary>
    /// Publishes a join-tenant receipt's outcome during replay, unless it is already published.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    /// <returns>Awaitable task.</returns>
    [Replay]
    public Task OnReplay(JoinTenantInvitationReceived @event, EventContext context) =>
        _issuance.Receive(@event, context);

    /// <summary>
    /// Issues a create-tenant token and forwards it to the outbox, or defers it while no signing key is configured.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    /// <returns>The deferral, when no signing key is configured.</returns>
    [OnceOnly]
    public Task<InvitationTokenIssuanceDeferred?> On(CreateTenantInvitationReceived @event, EventContext context) =>
        _issuance.Receive(@event, context);

    /// <summary>
    /// Publishes a create-tenant receipt's outcome during replay, unless it is already published.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    /// <returns>Awaitable task.</returns>
    [Replay]
    public Task OnReplay(CreateTenantInvitationReceived @event, EventContext context) =>
        _issuance.Receive(@event, context);

    /// <summary>
    /// Issues a fresh token for a pending invitation the host asked to reissue, for the same flow and
    /// recipient as the original receipt, or defers it while no signing key is configured.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    /// <returns>The deferral, when no signing key is configured.</returns>
    [OnceOnly]
    public Task<InvitationTokenIssuanceDeferred?> On(InvitationReissueReceived @event, EventContext context) => _issuance.Reissue(context);

    /// <summary>
    /// Publishes a reissue request's outcome during replay, unless it is already published.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    /// <returns>Awaitable task.</returns>
    [Replay]
    public Task OnReplay(InvitationReissueReceived @event, EventContext context) => _issuance.Reissue(context);

    /// <summary>
    /// Issues the token a deferred receipt or reissue request is waiting for, now that a signing key is configured.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    /// <returns>The deferral, when this instance still has no signing key.</returns>
    [OnceOnly]
    public Task<InvitationTokenIssuanceDeferred?> On(InvitationTokenIssuanceResumed @event, EventContext context) => _issuance.Resume(@event, context);

    /// <summary>
    /// Publishes a resumed issuance's outcome during replay, unless it is already published.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    /// <returns>Awaitable task.</returns>
    [Replay]
    public Task OnReplay(InvitationTokenIssuanceResumed @event, EventContext context) => _issuance.Resume(@event, context);
}
