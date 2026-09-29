// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;
using Ante.Integration.Replay.given;

namespace Ante.Integration.Replay.when_replaying_the_publishing_reactors;

/// <summary>
/// Replaying every reactor that publishes to Ante's outbox - the five forwarding reactors and the token-issuing
/// reactor - from the first event, after every onboarding has published, redelivers every event to them and publishes
/// nothing again: the outbox and what the host received stay exactly as they were, event for event (Cratis/Ante#120).
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_every_onboarding_has_completed : an_onboarding_history
{
    readonly Dictionary<string, (ulong Handled, int Observed)> _replayed = new(StringComparer.Ordinal);
    IReadOnlyList<string> _outboxBefore;
    IReadOnlyList<string> _receiptBefore;
    IReadOnlyList<string> _outboxAfter;
    IReadOnlyList<string> _receiptAfter;

    async Task Because()
    {
        _outboxBefore = await OutboxSnapshot();
        _receiptBefore = await Snapshots.Of(Host.InboxFromAnte);

        foreach (var reactor in PublishingReactors)
        {
            var observer = await KernelObservers.Replay(Ante, await ReactorId(reactor));
            _replayed[reactor.Name] = (observer.HandledEventCount, await KernelObservers.EventsObservedBy(Ante, observer));
        }

        _outboxAfter = await OutboxSnapshot();

        // Nothing new is in the outbox, so nothing new is in flight to the host; read it once every reactor is done.
        _receiptAfter = await Snapshots.Of(Host.InboxFromAnte);
    }

    [Fact] void should_replay_every_publishing_reactor() => _replayed.Keys.Order(StringComparer.Ordinal).ShouldContainOnly(PublishingReactors.Select(reactor => reactor.Name));
    [Fact] void should_redeliver_every_event_each_reactor_observes() =>
        _replayed.Where(entry => entry.Value.Observed == 0 || entry.Value.Handled != (ulong)entry.Value.Observed)
            .Select(entry => $"{entry.Key}: handled {entry.Value.Handled} of {entry.Value.Observed}").ShouldBeEmpty();
    [Fact] void should_not_publish_anything_again() => Snapshots.Text(_outboxAfter).ShouldEqual(Snapshots.Text(_outboxBefore));
    [Fact] void should_not_deliver_anything_again_to_the_host() => Snapshots.Text(_receiptAfter).ShouldEqual(Snapshots.Text(_receiptBefore));
}
