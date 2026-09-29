// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;
using Ante.Integration.Replay.given;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Integration.Replay.when_the_publishing_reactors_lose_their_checkpoints;

/// <summary>
/// A publishing reactor whose checkpoint is lost - its observer removed from the kernel while Ante is stopped, so the
/// next start observes the whole event log again as ordinary deliveries, not as a replay - finds every fact it would
/// publish already published, and publishes nothing again (Cratis/Ante#120).
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_every_onboarding_has_completed : an_onboarding_history
{
    readonly Dictionary<string, (ulong Handled, int Observed)> _redelivered = new(StringComparer.Ordinal);
    readonly Dictionary<string, ulong> _handledBefore = new(StringComparer.Ordinal);
    IReadOnlyList<string> _outboxBefore;
    IReadOnlyList<string> _receiptBefore;
    IReadOnlyList<string> _outboxAfter;
    IReadOnlyList<string> _receiptAfter;

    async Task Because()
    {
        var observers = new Dictionary<string, Cratis.Chronicle.Contracts.Observation.ObserverInformation>(StringComparer.Ordinal);
        foreach (var reactor in PublishingReactors)
        {
            var observer = await KernelObservers.WaitUntilCaughtUp(Ante, await ReactorId(reactor));
            observers[reactor.Name] = observer;
            _handledBefore[reactor.Name] = observer.HandledEventCount;
        }

        string eventStore;
        string @namespace;
        await using (var scope = Ante.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
            (eventStore, @namespace) = (store.Name.Value, store.Namespace.Value);
        }

        _outboxBefore = await OutboxSnapshot();
        _receiptBefore = await Snapshots.Of(Host.InboxFromAnte);

        await Restart(whileStopped: async () =>
        {
            foreach (var observer in observers.Values)
            {
                await KernelObservers.Remove(Host.KernelServices, eventStore, @namespace, observer.Id, observer.EventSequenceId);
            }
        });

        foreach (var (reactor, observer) in observers)
        {
            var restarted = await KernelObservers.WaitUntilCaughtUp(Ante, observer.Id);
            _redelivered[reactor] = (restarted.HandledEventCount, await KernelObservers.EventsObservedBy(Ante, restarted));
        }

        _outboxAfter = await OutboxSnapshot();
        _receiptAfter = await Snapshots.Of(Host.InboxFromAnte);
    }

    [Fact] void should_observe_the_whole_event_log_again() =>
        _redelivered.Where(entry => entry.Value.Observed == 0 || entry.Value.Handled != (ulong)entry.Value.Observed)
            .Select(entry => $"{entry.Key}: handled {entry.Value.Handled} of {entry.Value.Observed} (before the loss {_handledBefore[entry.Key]})").ShouldBeEmpty();
    [Fact] void should_restart_every_publishing_reactor() => _redelivered.Keys.ShouldContainOnly(PublishingReactors.Select(reactor => reactor.Name));
    [Fact] void should_not_publish_anything_again() => Snapshots.Text(_outboxAfter).ShouldEqual(Snapshots.Text(_outboxBefore));
    [Fact] void should_not_deliver_anything_again_to_the_host() => Snapshots.Text(_receiptAfter).ShouldEqual(Snapshots.Text(_receiptBefore));
}
