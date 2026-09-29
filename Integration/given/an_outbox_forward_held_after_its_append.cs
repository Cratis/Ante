// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Outbox;
using Cratis.Chronicle.Reactors;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Ante.Integration.given;

/// <summary>
/// Two Ante instances whose <typeparamref name="TReactor"/> outbox reactor can be stopped mid-forward: after its outbox
/// append of <typeparamref name="TFact"/>, before it has acknowledged the event (Cratis/Ante#119, #135).
/// </summary>
/// <remarks>
/// <para>
/// The forward is held at the one seam that runs after the append and before the acknowledgement: the status notifiers,
/// which <see cref="OutboxForwarder"/> calls once the fact is published. Each instance builds <typeparamref name="TReactor"/>
/// with a holding notifier ahead of the registered ones - Chronicle resolves a reactor from the service provider before
/// creating it - so only this reactor's forward is held, not another reactor forwarding a different fact for the same
/// event source.
/// </para>
/// <para>
/// Both instances install the hold and only the first forward of the event source is held, so the specification does
/// not depend on which instance Chronicle picks. Stopping that instance makes Chronicle hand the unacknowledged event to
/// the remaining one, which must neither publish the fact again nor fail its partition.
/// </para>
/// </remarks>
/// <typeparam name="TReactor">The outbox reactor to hold.</typeparam>
/// <typeparam name="TFact">The fact it forwards.</typeparam>
public class an_outbox_forward_held_after_its_append<TReactor, TFact> : two_running_antes
    where TReactor : class, IReactor
{
    readonly ForwardHold _hold = new();

    /// <summary>Gets how many facts were in the outbox while the forwarding instance was held.</summary>
    protected int PublishedWhileHeld { get; private set; }

    /// <summary>Gets whether the forwarding instance stopped while its handler was held.</summary>
    protected bool StoppedWithinTimeout { get; private set; }

    /// <summary>Gets how many facts are in the outbox once the remaining instance handled the redelivery.</summary>
    protected int Published { get; private set; }

    /// <summary>Gets how many facts reached the host.</summary>
    protected int ReceivedByHost { get; private set; }

    /// <summary>Gets how many partitions of the reactor failed on the remaining instance.</summary>
    protected int FailedPartitions { get; private set; }

    protected override Action<IServiceCollection>? ConfigureServices => services =>
        services.Replace(ServiceDescriptor.Transient(provider => ActivatorUtilities.CreateInstance<TReactor>(
            provider,
            new HeldNotifiers(
                provider.GetRequiredService<IInstancesOf<IPublicationStatusNotifier>>(),
                _hold,
                provider.GetRequiredService<IHostApplicationLifetime>()))));

    /// <summary>
    /// Starts the flow that records the fact, stops whichever instance holds the forward, and waits until the remaining
    /// instance has handled the redelivered event - or failed it.
    /// </summary>
    /// <param name="eventSourceId">The event source the fact is recorded for.</param>
    /// <param name="record">Runs the flow that records the fact to Ante's event log.</param>
    /// <returns>Awaitable task.</returns>
    protected async Task StopTheForwardingInstanceMidForward(string eventSourceId, Func<Task> record)
    {
        _hold.EventSourceId = eventSourceId;
        await record();

        var holder = await _hold.Held.Task.WaitAsync(Eventually.DefaultTimeout);
        var forwarding = ReferenceEquals(holder, Ante.Services.GetRequiredService<IHostApplicationLifetime>()) ? Ante : Other;
        var remaining = ReferenceEquals(forwarding, Ante) ? Other : Ante;
        PublishedWhileHeld = (await EventsFor(remaining, eventSourceId, EventSequenceId.Outbox, typeof(TFact))).Count;

        // Stop the forwarding instance while its handler is still inside the forward, then let the abandoned handler go.
        var stopping = forwarding.DisposeAsync().AsTask();
        StoppedWithinTimeout = await Task.WhenAny(stopping, Task.Delay(Eventually.DefaultTimeout)) == stopping;
        _hold.Release();
        await stopping;

        // The remaining instance is now the only target; once its reactor has handled the redelivered fact - or failed
        // it - any second forward has happened.
        var recorded = (await EventsFor(remaining, eventSourceId, EventSequenceId.Log, typeof(TFact)))[0].Context.SequenceNumber;
        await Eventually.Until(
            async () =>
            {
                await using var scope = remaining.Services.CreateAsyncScope();
                var reactors = scope.ServiceProvider.GetRequiredService<IEventStore>().Reactors;
                var state = await reactors.GetStateFor<TReactor>();
                FailedPartitions = (await reactors.GetFailedPartitionsFor<TReactor>()).Count();
                return FailedPartitions > 0 || (state.LastHandledEventSequenceNumber.IsActualValue && state.LastHandledEventSequenceNumber >= recorded);
            },
            what: $"the remaining instance's {typeof(TReactor).Name} to have handled or failed the {typeof(TFact).Name}");

        Published = (await EventsFor(remaining, eventSourceId, EventSequenceId.Outbox, typeof(TFact))).Count;
        await Host.WaitForFromAnte<TFact>(eventSourceId);
        ReceivedByHost = (await Host.ReceivedFromAnte(eventSourceId)).Count(entry => entry.Content is TFact);
    }

    /// <summary>Holds the first forward of one event source until released, and records which instance holds it.</summary>
    sealed class ForwardHold
    {
        readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int _taken;

        public string EventSourceId { get; set; } = string.Empty;

        public TaskCompletionSource<IHostApplicationLifetime> Held { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task HoldIfFirst(EventSourceId eventSourceId, IHostApplicationLifetime instance)
        {
            if (eventSourceId.Value != EventSourceId || Interlocked.Exchange(ref _taken, 1) == 1)
            {
                return Task.CompletedTask;
            }

            Held.SetResult(instance);
            return _release.Task;
        }

        public void Release() => _release.TrySetResult();
    }

    /// <summary>The registered notifiers, after one that holds the first forward.</summary>
    sealed class HeldNotifiers(IInstancesOf<IPublicationStatusNotifier> notifiers, ForwardHold hold, IHostApplicationLifetime instance)
        : IInstancesOf<IPublicationStatusNotifier>
    {
        public IEnumerator<IPublicationStatusNotifier> GetEnumerator() =>
            notifiers.Prepend(new HoldingNotifier(hold, instance)).GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    sealed class HoldingNotifier(ForwardHold hold, IHostApplicationLifetime instance) : IPublicationStatusNotifier
    {
        public Task NotifyIfPublished(EventSourceId eventSourceId) => hold.HoldIfFirst(eventSourceId, instance);

        public Task NotifyRecorded(EventSourceId eventSourceId, object @event) => Task.CompletedTask;
    }
}
