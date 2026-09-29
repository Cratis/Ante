// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using MongoDB.Driver;

namespace Ante.Invitations.Issuing.for_DeferredInvitationTokenUpgradeWindow.when_loading;

public class and_the_activation_was_recorded : Specification
{
    readonly DateTimeOffset _now = DateTimeOffset.UtcNow;
    IMongoDatabase _database = null!;
    DeferredInvitationTokenUpgradeWindow _window = null!;

    void Establish()
    {
        var collection = Substitute.For<IMongoCollection<InvitationTokenIsolationActivation>>();
        collection.FindOneAndUpdateAsync(
            Arg.Any<FilterDefinition<InvitationTokenIsolationActivation>>(),
            Arg.Any<UpdateDefinition<InvitationTokenIsolationActivation>>(),
            Arg.Any<FindOneAndUpdateOptions<InvitationTokenIsolationActivation, InvitationTokenIsolationActivation>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new InvitationTokenIsolationActivation(InvitationTokenIsolationActivation.Singleton, _now) { LegacyUntil = _now + InvitationTokenUpgradeWindow.RolloutGrace + TimeSpan.FromDays(7) }));
        _database = Substitute.For<IMongoDatabase>();
        _database.GetCollection<InvitationTokenIsolationActivation>("invitation-token-isolation", Arg.Any<MongoCollectionSettings>()).Returns(collection);
        _window = new DeferredInvitationTokenUpgradeWindow(TimeSpan.FromDays(7), TimeProvider.System);
    }

    async Task Because() => await _window.Load(_database);

    [Fact] void should_be_loaded() => _window.IsLoaded.ShouldBeTrue();
    [Fact] void should_accept_a_token_issued_at_activation() =>
        _window.AcceptsLegacyToken(_now, _now.AddDays(1), _now).ShouldBeTrue();
}
#endif
