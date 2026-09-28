// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Invitations.Receiving;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Ante.Invitations.Accepting;

/// <summary>
/// Reads the authoritative invitation revision that a command must guard at append time.
/// </summary>
public interface IInvitationAcceptanceFence
{
    /// <summary>
    /// Returns a concurrency scope only for an existing, pending invitation of the expected flow.
    /// </summary>
    /// <param name="invitationId">The invitation.</param>
    /// <param name="expectedFlow">The command's flow.</param>
    /// <returns>The append scope, or null when the invitation is no longer pending.</returns>
    Task<ConcurrencyScope?> For(InvitationId invitationId, InvitationFlowType expectedFlow);
}

/// <summary>
/// Fences acceptance against a local revocation even when the pending projection has not caught up.
/// </summary>
/// <param name="store">The local Chronicle store.</param>
public class InvitationAcceptanceFence(IEventStore store) : IInvitationAcceptanceFence
{
    static readonly EventType[] _decisionTypes =
    [
        typeof(JoinTenantInvitationReceived).GetEventType(),
        typeof(CreateTenantInvitationReceived).GetEventType(),
        typeof(InvitationRevocationReceived).GetEventType(),
        typeof(InvitationToJoinTenantAccepted).GetEventType(),
        typeof(InvitationToCreateTenantAccepted).GetEventType(),
    ];

    /// <summary>
    /// Reads the authoritative invitation stream and returns the exact revision guarded at append time.
    /// </summary>
    /// <param name="invitationId">The invitation's event source.</param>
    /// <param name="expectedFlow">The received flow required by this command.</param>
    /// <returns>A scope, or null if this invitation is missing, has been revoked or already accepted.</returns>
    public async Task<ConcurrencyScope?> For(InvitationId invitationId, InvitationFlowType expectedFlow)
    {
        var source = (EventSourceId)invitationId.Value.ToString("D");
        var history = await store.EventLog.GetForEventSourceIdAndEventTypes(source, _decisionTypes);
        var receipts = history.Where(entry => entry.Content is JoinTenantInvitationReceived or CreateTenantInvitationReceived).ToArray();
        var joinExpected = expectedFlow == InvitationFlowType.JoinTenant;
        if (receipts.Length != 1 ||
            joinExpected != (receipts[0].Content is JoinTenantInvitationReceived) ||
            history.Any(entry => entry.Content is InvitationRevocationReceived or InvitationToJoinTenantAccepted or InvitationToCreateTenantAccepted))
        {
            return null;
        }

        // Both the read and append cover revocation. If revocation wins the race the kernel refuses
        // the acceptance batch, including its legal and attempt facts, rather than trusting a stale view.
        return new(history[^1].Context.SequenceNumber, source, EventTypes: _decisionTypes);
    }
}
