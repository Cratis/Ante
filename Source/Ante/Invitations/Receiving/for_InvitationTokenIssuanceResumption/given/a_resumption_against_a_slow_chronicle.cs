// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Ante.Invitations.Issuing;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.Receiving.for_InvitationTokenIssuanceResumption.given;

/// <summary>
/// A resumption on an instance with a signing key, whose Chronicle read of the waiting invitations never answers
/// until <see cref="ReadsBeforeChronicleResponds"/> reads have been made. Passes are short so a timed-out pass is
/// retried within the spec's deadline instead of after the production interval.
/// </summary>
public class a_resumption_against_a_slow_chronicle : Specification
{
    protected static readonly InvitationId Invitation = InvitationId.New();
    protected static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
    protected readonly ServiceCollection Services = new();
    protected readonly TaskCompletionSource Retried = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource Appended = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected int ReadsBeforeChronicleResponds = int.MaxValue;
    protected int Reads;
    protected IEventLog Log = null!;
    protected ServiceProvider Provider = null!;
    protected InvitationTokenIssuanceResumption Resumption = null!;

    void Establish()
    {
        var eventStore = Substitute.For<IEventStore>();
        var readModels = Substitute.For<IReadModels>();
        Log = Substitute.For<IEventLog>();
        eventStore.ReadModels.Returns(readModels);
        eventStore.EventLog.Returns(Log);
        readModels.GetInstances<InvitationAwaitingSigningKey>(Arg.Any<EventCount?>()).Returns(_ => Read());
        Log.GetForEventSourceIdAndEventTypes(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventSourceType>())
            .Returns(Task.FromResult<IImmutableList<AppendedEvent>>(ImmutableList.Create(
                new AppendedEvent(EventContext.Empty with { EventSourceId = Invitation, SequenceNumber = 4 }, new InvitationTokenIssuanceDeferred(3)))));
        Log.Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<ConcurrencyScope>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Cratis.Chronicle.Subject>())
            .Returns(_ =>
            {
                Appended.TrySetResult();
                return AppendResult.Success(CorrelationId.New(), 5);
            });
        Services.AddLogging();
        Services.AddSingleton(eventStore);
        Services.AddScoped<InvitationTokenIssuanceResumer>();
        Services.Configure<InvitationTokenConfig>(config => config.PrivateKeyPem = "a configured key");
    }

    protected void Build()
    {
        Provider = Services.BuildServiceProvider();
        Resumption = new(
            Provider.GetRequiredService<IServiceScopeFactory>(),
            Provider.GetRequiredService<IOptions<InvitationTokenConfig>>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<InvitationTokenIssuanceResumption>.Instance,
            interval: TimeSpan.FromMilliseconds(50),
            passTimeout: TimeSpan.FromMilliseconds(100));
    }

    protected async Task Stop()
    {
        await Resumption.StopAsync(CancellationToken.None);
        Resumption.Dispose();
        await Provider.DisposeAsync();
    }

    Task<IEnumerable<InvitationAwaitingSigningKey>> Read()
    {
        var read = Interlocked.Increment(ref Reads);
        if (read > 1)
        {
            Retried.TrySetResult();
        }

        return read > ReadsBeforeChronicleResponds
            ? Task.FromResult<IEnumerable<InvitationAwaitingSigningKey>>([new(Invitation, 3)])
            : new TaskCompletionSource<IEnumerable<InvitationAwaitingSigningKey>>().Task;
    }
}
#endif
