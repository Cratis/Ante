// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Ante.Invitations.Issuing.for_InvitationTokenUpgradeWindow.when_loading.given;

public class an_activation_store : Specification
{
    protected static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
    protected static readonly TimeSpan Expiry = TimeSpan.FromDays(7);

    protected IMongoDatabase Database = null!;
    protected UpdateDefinition<InvitationTokenIsolationActivation>? Update;

    // What the store already holds; null means this is the first run and the upsert inserts.
    protected virtual InvitationTokenIsolationActivation? Existing => null;

    void Establish()
    {
        var collection = Substitute.For<IMongoCollection<InvitationTokenIsolationActivation>>();
        collection.FindOneAndUpdateAsync(
            Arg.Any<FilterDefinition<InvitationTokenIsolationActivation>>(),
            Arg.Do<UpdateDefinition<InvitationTokenIsolationActivation>>(update => Update = update),
            Arg.Any<FindOneAndUpdateOptions<InvitationTokenIsolationActivation, InvitationTokenIsolationActivation>>(),
            Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(Existing ?? new InvitationTokenIsolationActivation(InvitationTokenIsolationActivation.Singleton, Now) { LegacyUntil = Now + Expiry }));

        Database = Substitute.For<IMongoDatabase>();
        Database.GetCollection<InvitationTokenIsolationActivation>("invitation-token-isolation", Arg.Any<MongoCollectionSettings>()).Returns(collection);
    }

    protected BsonDocument RenderedUpdate() => Update!.Render(new RenderArgs<InvitationTokenIsolationActivation>(
        BsonSerializer.SerializerRegistry.GetSerializer<InvitationTokenIsolationActivation>(),
        BsonSerializer.SerializerRegistry)).AsBsonDocument;
}
#endif
