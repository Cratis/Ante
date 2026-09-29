// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Ante.Integration.given;

/// <summary>
/// Two Ante instances - replicas of one deployment - on the same Chronicle kernel, event store and namespace, MongoDB
/// database, host stores and signing key (Cratis/Ante#119).
/// </summary>
/// <remarks>
/// <para>
/// What Chronicle 19.22 does with two connected instances of the same client decides what these specs can assert.
/// Every reactor (and reducer) has one observer grain per event store, namespace and event sequence in the kernel, and
/// that grain owns the cursor. When the second instance registers the same reactor with an identical definition the
/// grain adds it as a fan-out target instead of replacing the subscription (<c>Observer.Subscribe</c> and
/// <c>CanFanOutInto</c>). Each batch is delivered to exactly one target: the default <c>round-robin</c> strategy
/// (<c>RoundRobinObserverSubscriberSelector</c>) picks it from a stable hash of the partition - the event source id -
/// over the targets ordered by connection id, so one invitation's events stick to one instance while both are
/// connected. The reactor therefore runs once per event across the deployment, not once per instance.
/// </para>
/// <para>
/// Delivery is still at least once. When the chosen instance answers that it has disconnected, the grain drops it
/// and retries the batch on a remaining instance (<c>Observer.Handling</c>); when it does not answer in time the call
/// is abandoned (<c>ObserverSubscriberExtensions.OnNextWithin</c>) and the partition is retried later. Either way an
/// instance that stops mid-handler can have done part or all of the work before the batch is handed to the other.
/// Projections are kernel-owned and run once in the kernel whatever the number of clients.
/// </para>
/// <para>
/// Everything in memory stays per instance: a live status subscription only hears the notifications of reactors that
/// ran on its own instance, and is otherwise seeded from durable facts when it is opened.
/// </para>
/// </remarks>
public class two_running_antes : a_running_ante
{
    protected AnteApplication Other;

    async Task Establish() => Other = await StartAnotherInstance();

    /// <summary>
    /// Reads events of the given types for one event source from the shared store, through any running instance.
    /// </summary>
    protected static async Task<IReadOnlyList<AppendedEvent>> EventsFor(AnteApplication through, string eventSourceId, EventSequenceId sequence, params Type[] eventTypes)
    {
        await using var scope = through.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
        return await store.GetEventSequence(sequence).GetForEventSourceIdAndEventTypes(eventSourceId, [.. eventTypes.Select(type => type.GetEventType())]);
    }
}
