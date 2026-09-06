// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Legal;

namespace Ante.Legal;

/// <summary>
/// Forwards <see cref="LegalTermsAccepted"/> to the outbox so the host can keep its own compliance
/// record of who accepted what, and when.
/// </summary>
/// <remarks>
/// All three onboarding flows - self-service registration, setting up an organization from an
/// invitation, and joining an existing one - append the same acceptance event, so one reactor covers
/// all of them rather than each slice growing its own copy.
/// </remarks>
/// <param name="eventStore">The event store.</param>
[Reactor]
public class LegalTermsAcceptanceOutbox(IEventStore eventStore) : IReactor
{
    /// <summary>
    /// Forwards the acceptance to the outbox.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task On(LegalTermsAccepted @event, EventContext context) =>
        await eventStore.GetEventSequence(EventSequenceId.Outbox).Append(context.EventSourceId, @event);
}
