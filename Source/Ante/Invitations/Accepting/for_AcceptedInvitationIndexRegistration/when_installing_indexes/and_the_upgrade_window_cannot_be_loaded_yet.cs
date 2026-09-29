// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Issuing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_AcceptedInvitationIndexRegistration.when_installing_indexes;

public class and_the_upgrade_window_cannot_be_loaded_yet : Specification
{
    readonly TaskCompletionSource _loadAttempted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ServiceProvider _provider = null!;
    AcceptedInvitationIndexRegistration _registration = null!;
    DeferredInvitationTokenUpgradeWindow _window = null!;
    int _loads;
    bool _readyAfterFirstFailure;
    bool _readyAfterRecovery;

    void Establish()
    {
        var activations = Substitute.For<IMongoCollection<InvitationTokenIsolationActivation>>();
        activations.FindOneAndUpdateAsync(
            Arg.Any<FilterDefinition<InvitationTokenIsolationActivation>>(),
            Arg.Any<UpdateDefinition<InvitationTokenIsolationActivation>>(),
            Arg.Any<FindOneAndUpdateOptions<InvitationTokenIsolationActivation, InvitationTokenIsolationActivation>>(),
            Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (Interlocked.Increment(ref _loads) == 1)
                {
                    _loadAttempted.TrySetResult();
                    return Task.FromException<InvitationTokenIsolationActivation>(new TimeoutException());
                }

                return Task.FromResult(new InvitationTokenIsolationActivation(InvitationTokenIsolationActivation.Singleton, DateTimeOffset.UtcNow));
            });
        var database = Substitute.For<IMongoDatabase>();
        database.GetCollection<InvitationTokenIsolationActivation>("invitation-token-isolation", Arg.Any<MongoCollectionSettings>()).Returns(activations);
        var indexes = Substitute.For<IMongoIndexManager<AcceptedInvitation>>();
        indexes.CreateManyAsync(Arg.Any<IEnumerable<CreateIndexModel<AcceptedInvitation>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>(["UniqueAcceptedInvitationSession", "AcceptedInvitationExpiry"]));
        var collection = Substitute.For<IMongoCollection<AcceptedInvitation>>();
        collection.Indexes.Returns(indexes);
        collection.Database.Returns(database);
        _provider = new ServiceCollection().AddSingleton(collection).BuildServiceProvider();
        _window = new DeferredInvitationTokenUpgradeWindow(TimeSpan.FromDays(7), TimeProvider.System);
        _registration = new(
            _provider.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AcceptedInvitationIndexRegistration>.Instance,
            upgradeWindow: _window);
    }

    async Task Because()
    {
        await _registration.StartAsync(CancellationToken.None);
        await _loadAttempted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _readyAfterFirstFailure = _registration.IsReady;
        await _registration.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(10));
        _readyAfterRecovery = _registration.IsReady;
    }

    async Task Destroy()
    {
        await _registration.StopAsync(CancellationToken.None);
        await _provider.DisposeAsync();
    }

    [Fact] void should_not_be_ready_while_the_window_is_unloaded() => _readyAfterFirstFailure.ShouldBeFalse();
    [Fact] void should_retry_the_load() => _loads.ShouldEqual(2);
    [Fact] void should_be_ready_once_the_window_is_loaded() => _readyAfterRecovery.ShouldBeTrue();
    [Fact] void should_have_loaded_the_window() => _window.IsLoaded.ShouldBeTrue();
}
#endif
