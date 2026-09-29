// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AcceptedInvitationIndexRegistration.given;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_AcceptedInvitationIndexRegistration.when_installing_indexes;

public class and_a_transient_server_error_occurs : Specification
{
    readonly TaskCompletionSource _retried = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ServiceProvider _provider = null!;
    AcceptedInvitationIndexRegistration _registration = null!;
    int _attempts;
    bool _ready;

    void Establish()
    {
        var indexes = Substitute.For<IMongoIndexManager<AcceptedInvitation>>();
        indexes.CreateManyAsync(Arg.Any<IEnumerable<CreateIndexModel<AcceptedInvitation>>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (Interlocked.Increment(ref _attempts) == 1)
                {
                    return Task.FromException<IEnumerable<string>>(
                        an_index_error.Recovering());
                }

                _retried.TrySetResult();
                return Task.FromResult<IEnumerable<string>>(["UniqueAcceptedInvitationSession", "AcceptedInvitationExpiry"]);
            });
        var collection = Substitute.For<IMongoCollection<AcceptedInvitation>>();
        collection.Indexes.Returns(indexes);
        _provider = new ServiceCollection().AddSingleton(collection).BuildServiceProvider();
        _registration = new(_provider.GetRequiredService<IServiceScopeFactory>(), Microsoft.Extensions.Logging.Abstractions.NullLogger<AcceptedInvitationIndexRegistration>.Instance);
    }

    async Task Because()
    {
        await _registration.StartAsync(CancellationToken.None);
        await _retried.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _registration.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));
        _ready = _registration.IsReady;
    }

    async Task Destroy()
    {
        await _registration.StopAsync(CancellationToken.None);
        await _provider.DisposeAsync();
    }

    [Fact] void should_retry_the_index_command() => _attempts.ShouldEqual(2);
    [Fact] void should_only_become_ready_after_success() => _ready.ShouldBeTrue();
}
#endif
