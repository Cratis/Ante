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
/// <para>
/// All three onboarding flows - self-service registration, setting up an organization from an
/// invitation, and joining an existing one - append the same acceptance event, so one reactor covers
/// all of them rather than each slice growing its own copy. Because it is shared, it cannot know by
/// itself which flow a given invitation or registration id belongs to, so it hands every registered
/// <see cref="IPublicationStatusNotifier"/> a chance to check - the ones that do not recognize the id do
/// nothing. This is what closes the live-status gap for the (uncommon) case where the legal fact reaches
/// the outbox after the flow's own accept/registration fact rather than before it.
/// </para>
/// <para>
/// The local event log is named through <see cref="ReactorAttribute"/>'s event sequence rather than
/// <c language="csharp">[EventLog]</c>:
/// Chronicle's client treats any type carrying an <c language="csharp">EventSequenceAttribute</c> as a model-bound
/// projection too, registers a projection under this reactor's id with no event types, and that projection takes
/// over the observer - the reactor then never receives an event. The end-to-end fixture in
/// <c language="csharp">Integration/</c> caught this against a real kernel; see Cratis/Ante#72 and Cratis/Chronicle#4310.
/// </para>
/// </remarks>
/// <param name="eventStore">The event store.</param>
/// <param name="notifiers">Every registered <see cref="IPublicationStatusNotifier"/>.</param>
/// <param name="logger">The logger.</param>
[Reactor(eventSequence: EventSequenceId.LogId)]
public class LegalTermsAcceptanceOutbox(IEventStore eventStore, IInstancesOf<IPublicationStatusNotifier> notifiers, ILogger<LegalTermsAcceptanceOutbox> logger) : IReactor
{
    public Task On(LegalTermsAcceptanceRecorded @event, EventContext context, ReactorDelivery delivery) =>
        eventStore.PublishToOutbox(delivery, context, @event.ToContract(), notifiers, logger);

    /// <summary>
    /// Forwards the acceptance to the outbox.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    /// <param name="delivery">The delivery being handled; a redelivery of it does not publish the fact again.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task On(LegalTermsAccepted @event, EventContext context, ReactorDelivery delivery) =>
        await eventStore.PublishToOutbox(delivery, context, @event, notifiers, logger);
}
