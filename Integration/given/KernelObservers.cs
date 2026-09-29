// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Kernel = Cratis.Chronicle.Contracts;

namespace Ante.Integration.given;

/// <summary>
/// What an operator does to Ante's observers through the kernel's Observers service - the service the Workbench and the
/// <c>cratis chronicle</c> CLI use - with every wait tied to the kernel's own record of the observer instead of a sleep.
/// </summary>
/// <remarks>
/// <para>
/// Chronicle 19.22 (<c>Source/Kernel/Grpc/Observation/Observers.cs</c>) resolves an observer by id, event store,
/// namespace and event sequence; an empty event sequence means the event log. The client's own
/// <c>IReactors.Replay</c> and <c>IProjections.Replay</c> always send an empty sequence, so they cannot reach an
/// observer of the outbox - these helpers look the sequence up first and send it.
/// </para>
/// <para>
/// <c>Replay</c> moves the observer into its <c>Replay</c> state, which starts a <c>ReplayObserver</c> job from the first
/// event and resets the handled count (<c>Observer.Replay.cs</c>, <c>States/Replay.cs</c>); the job ends with the
/// observer back in routing and its cursor at the last event it handled. <c>RemoveObserver</c> deletes the observer's
/// state, cursor, failed partitions, handled counts and jobs, refusing while a client is subscribed
/// (<c>ObserverRemover.cs</c>) - a lost checkpoint. The next registration then starts a new observer from the first event,
/// delivered as ordinary events rather than as a replay.
/// </para>
/// </remarks>
public static class KernelObservers
{
    /// <summary>
    /// Gets the kernel's record of one of Ante's observers.
    /// </summary>
    /// <param name="ante">The running Ante.</param>
    /// <param name="observerId">The observer (reactor or projection) id.</param>
    /// <returns>The observer as the kernel reports it.</returns>
    public static async Task<Kernel.Observation.ObserverInformation> Get(AnteApplication ante, string observerId)
    {
        await using var scope = ante.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
        return await Get(Services(store), store.Name.Value, store.Namespace.Value, observerId);
    }

    /// <summary>
    /// Gets the kernel's record of an observer in any event store.
    /// </summary>
    /// <param name="services">A connection's kernel services.</param>
    /// <param name="eventStore">The event store the observer belongs to.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="observerId">The observer id.</param>
    /// <returns>The observer as the kernel reports it.</returns>
    public static async Task<Kernel.Observation.ObserverInformation> Get(Kernel.IServices services, string eventStore, string @namespace, string observerId)
    {
        var observers = (await services.Observers.GetObservers(new() { EventStore = eventStore, Namespace = @namespace })).ToArray();
        return observers.SingleOrDefault(observer => observer.Id == observerId) ??
            throw new InvalidOperationException($"The kernel has no observer {observerId} in {eventStore}/{@namespace}; it has [{string.Join(", ", observers.Select(observer => observer.Id))}].");
    }

    /// <summary>
    /// Replays an observer of Ante from the first event and waits until the replay job has completed and the observer
    /// is active and has handled the last event of its types again.
    /// </summary>
    /// <remarks>
    /// An observer that was already caught up before the replay looks the same after it, so the wait insists on positive
    /// evidence that the replay ran: the replay job read back as completed successfully, or - since a completed job may be
    /// removed and then reads as no job - the job seen earlier, the observer seen replaying, or its handled count seen
    /// reset below what it was before. Without any of these within the timeout the replay counts as never having run.
    /// </remarks>
    /// <param name="ante">The running Ante.</param>
    /// <param name="observerId">The observer (reactor or projection) id.</param>
    /// <returns>The observer as the kernel reports it once the replay is complete.</returns>
    public static async Task<Kernel.Observation.ObserverInformation> Replay(AnteApplication ante, string observerId)
    {
        await using var scope = ante.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
        var observer = await Get(Services(store), store.Name.Value, store.Namespace.Value, observerId);
        var response = await Services(store).Observers.Replay(new()
        {
            EventStore = store.Name.Value,
            Namespace = store.Namespace.Value,
            ObserverId = observerId,
            EventSequenceId = observer.EventSequenceId,
        });
        if (!Guid.TryParse(response.JobId, out var id) || id == Guid.Empty)
        {
            throw new InvalidOperationException($"The kernel started no replay job for {observerId} (replayable: {observer.IsReplayable}).");
        }

        var jobId = new JobId(id);
        var jobSeen = false;
        var replayingSeen = false;
        var resetSeen = false;
        JobStatus? lastStatus = null;
        try
        {
            await Eventually.Until(
                async () =>
                {
                    var job = await store.Jobs.GetJob(jobId);
                    var current = await Get(Services(store), store.Name.Value, store.Namespace.Value, observerId);
                    replayingSeen |= current.RunningState == Kernel.Observation.ObserverRunningState.Replaying;
                    resetSeen |= current.HandledEventCount < observer.HandledEventCount;
                    if (job is null)
                    {
                        // No job: either not visible yet, or completed and removed - only the latter once it was seen running.
                        return jobSeen || replayingSeen || resetSeen;
                    }

                    jobSeen = true;
                    lastStatus = job.Status;
                    return job.Status switch
                    {
                        JobStatus.CompletedSuccessfully => true,
                        JobStatus.CompletedWithFailures or JobStatus.Failed or JobStatus.Stopped =>
                            throw new InvalidOperationException($"The replay job for {observerId} ended as {job.Status}."),
                        _ => false,
                    };
                },
                what: $"evidence that the replay of {observerId} ran and completed");
        }
        catch (TimeoutException timeout)
        {
            throw new TimeoutException(
                $"{timeout.Message} Replay job {jobId} seen: {jobSeen} (last status {lastStatus}); observer seen replaying: {replayingSeen}; handled count seen reset below {observer.HandledEventCount}: {resetSeen}.",
                timeout);
        }

        return await WaitUntilCaughtUp(ante, observerId);
    }

    /// <summary>
    /// Waits until an observer of Ante is active - not replaying, catching up or disconnected - and has handled the
    /// last event of its types in its event sequence.
    /// </summary>
    /// <param name="ante">The running Ante.</param>
    /// <param name="observerId">The observer (reactor or projection) id.</param>
    /// <returns>The observer as the kernel reports it once caught up.</returns>
    public static async Task<Kernel.Observation.ObserverInformation> WaitUntilCaughtUp(AnteApplication ante, string observerId)
    {
        await using var scope = ante.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
        Kernel.Observation.ObserverInformation? last = null;
        try
        {
            return await Eventually.Get(
                async () =>
                {
                    last = await Get(Services(store), store.Name.Value, store.Namespace.Value, observerId);
                    if (last.RunningState != Kernel.Observation.ObserverRunningState.Active)
                    {
                        return null;
                    }

                    var target = await LastOfTypes(store, last);
                    var handled = new EventSequenceNumber(last.LastHandledEventSequenceNumber);
                    return target is null || (handled.IsActualValue && handled >= target) ? last : null;
                },
                what: $"{observerId} to be active and caught up");
        }
        catch (TimeoutException timeout)
        {
            throw new TimeoutException(
                $"{timeout.Message} Last seen: state {last?.RunningState}, next {last?.NextEventSequenceNumber}, last handled {last?.LastHandledEventSequenceNumber}, handled count {last?.HandledEventCount}.",
                timeout);
        }
    }

    /// <summary>
    /// Counts the events an observer of Ante observes in its event sequence - what a replay or a restart from the first
    /// event delivers to it.
    /// </summary>
    /// <param name="ante">The running Ante.</param>
    /// <param name="observer">The observer.</param>
    /// <returns>The number of events of its types.</returns>
    public static async Task<int> EventsObservedBy(AnteApplication ante, Kernel.Observation.ObserverInformation observer)
    {
        await using var scope = ante.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
        return (await Observed(store, observer)).Count;
    }

    /// <summary>
    /// Removes an observer, with its cursor and everything else the kernel keys to it, once no client is subscribed to
    /// it; retries while the stopped client's subscription is still being torn down.
    /// </summary>
    /// <param name="services">A connection's kernel services - any client, since Ante is stopped.</param>
    /// <param name="eventStore">The event store the observer belongs to.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="observerId">The observer id.</param>
    /// <param name="eventSequenceId">The event sequence it observes.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task Remove(Kernel.IServices services, string eventStore, string @namespace, string observerId, string eventSequenceId)
    {
        Kernel.Observation.ObserverRemovalOutcome? outcome = null;
        try
        {
            await Eventually.Until(
                async () =>
                {
                    var response = await services.Observers.RemoveObserver(new()
                    {
                        EventStore = eventStore,
                        Namespace = @namespace,
                        ObserverId = observerId,
                        EventSequenceId = eventSequenceId,
                    });
                    outcome = response.Outcome;
                    return outcome switch
                    {
                        Kernel.Observation.ObserverRemovalOutcome.Removed => true,
                        Kernel.Observation.ObserverRemovalOutcome.ObserverNotFound =>
                            throw new InvalidOperationException($"The kernel has no observer {observerId} to remove."),
                        _ => false,
                    };
                },
                what: $"the kernel to remove {observerId}");
        }
        catch (TimeoutException timeout)
        {
            throw new TimeoutException($"{timeout.Message} Last outcome: {outcome}.", timeout);
        }

        // The removal reported, and the kernel no longer knows the observer: the next registration starts a new one with
        // no cursor and no handled count, so whatever it has handled afterwards it handled again from the beginning.
        var remaining = await services.Observers.GetObservers(new() { EventStore = eventStore, Namespace = @namespace });
        if (remaining.Any(observer => observer.Id == observerId))
        {
            throw new InvalidOperationException($"The kernel reported {observerId} removed but still lists it.");
        }
    }

    /// <summary>
    /// Gets the kernel services of a Chronicle connection.
    /// </summary>
    /// <param name="store">Any event store on the connection.</param>
    /// <returns>The kernel services.</returns>
    public static Kernel.IServices Services(IEventStore store) =>
        ((Kernel.IChronicleServicesAccessor)store.Connection).Services;

    static async Task<EventSequenceNumber?> LastOfTypes(IEventStore store, Kernel.Observation.ObserverInformation observer)
    {
        var observed = await Observed(store, observer);
        return observed.Count == 0 ? null : observed[^1].Context.SequenceNumber;
    }

    static async Task<IReadOnlyList<AppendedEvent>> Observed(IEventStore store, Kernel.Observation.ObserverInformation observer)
    {
        var types = observer.EventTypes.Select(type => type.Id).ToHashSet(StringComparer.Ordinal);
        var sequence = store.GetEventSequence(new EventSequenceId(observer.EventSequenceId));
        return [.. (await sequence.GetFromSequenceNumber(EventSequenceNumber.First)).Where(appended => types.Contains(appended.Context.EventType.Id.Value))];
    }
}
