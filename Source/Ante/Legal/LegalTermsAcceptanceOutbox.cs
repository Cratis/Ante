// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Legal;
using Ante.Outbox;
using Cratis.Types;

namespace Ante.Legal;

/// <summary>
/// Forwards <see cref="LegalTermsAccepted"/> to the outbox so the host can keep its own compliance
/// record of who accepted what, and when.
/// </summary>
/// <remarks>
/// All three onboarding flows - self-service registration, setting up an organization from an
/// invitation, and joining an existing one - append the same acceptance event, so one reactor covers
/// all of them rather than each slice growing its own copy. Because it is shared, it cannot know by
/// itself which flow a given invitation or registration id belongs to, so it hands every registered
/// <see cref="IPublicationStatusNotifier"/> a chance to check - the ones that do not recognize the id do
/// nothing. This is what closes the live-status gap for the (uncommon) case where the legal fact reaches
/// the outbox after the flow's own accept/registration fact rather than before it.
/// </remarks>
/// <param name="eventStore">The event store.</param>
/// <param name="notifiers">Every registered <see cref="IPublicationStatusNotifier"/>.</param>
[Reactor]
public class LegalTermsAcceptanceOutbox(IEventStore eventStore, IInstancesOf<IPublicationStatusNotifier> notifiers) : IReactor
{
    /// <summary>
    /// Forwards the acceptance to the outbox.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task On(LegalTermsAccepted @event, EventContext context) =>
        await eventStore.PublishToOutbox(context, @event, notifiers);
}
