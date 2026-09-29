// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using MongoDB.Driver;

namespace Ante.Invitations.Issuing.for_DeferredInvitationTokenUpgradeWindow.when_loading;

public class and_mongodb_is_unavailable : Specification
{
    readonly DateTimeOffset _now = DateTimeOffset.UtcNow;
    IMongoDatabase _database = null!;
    DeferredInvitationTokenUpgradeWindow _window = null!;
    Exception? _error;

    void Establish()
    {
        var collection = Substitute.For<IMongoCollection<InvitationTokenIsolationActivation>>();
        collection.FindOneAndUpdateAsync(
            Arg.Any<FilterDefinition<InvitationTokenIsolationActivation>>(),
            Arg.Any<UpdateDefinition<InvitationTokenIsolationActivation>>(),
            Arg.Any<FindOneAndUpdateOptions<InvitationTokenIsolationActivation, InvitationTokenIsolationActivation>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromException<InvitationTokenIsolationActivation>(new TimeoutException()));
        _database = Substitute.For<IMongoDatabase>();
        _database.GetCollection<InvitationTokenIsolationActivation>("invitation-token-isolation", Arg.Any<MongoCollectionSettings>()).Returns(collection);
        _window = new DeferredInvitationTokenUpgradeWindow(TimeSpan.FromDays(7), TimeProvider.System);
    }

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(() => _window.Load(_database));

    [Fact] void should_fail_so_the_caller_can_retry() => _error.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_not_be_loaded() => _window.IsLoaded.ShouldBeFalse();
    [Fact] void should_keep_refusing_tokens_without_issuer_and_audience() =>
        _window.AcceptsLegacyToken(_now, _now.AddDays(1), _now).ShouldBeFalse();
}
#endif
