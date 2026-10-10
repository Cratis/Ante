// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_AcceptedInvitationIndexRegistration.when_installing_indexes;

// The old index allowed one session per login, which is what let a second invitation replace the first.
public class and_the_legacy_single_session_index_exists : Specification
{
    IMongoIndexManager<AcceptedInvitation> _indexes = null!;
    IEnumerable<CreateIndexModel<AcceptedInvitation>> _created = [];

    void Establish()
    {
        _indexes = Substitute.For<IMongoIndexManager<AcceptedInvitation>>();
        _indexes.CreateManyAsync(Arg.Do<IEnumerable<CreateIndexModel<AcceptedInvitation>>>(models => _created = models.ToArray()), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>([]));
    }

    async Task Because()
    {
        var collection = Substitute.For<IMongoCollection<AcceptedInvitation>>();
        collection.Indexes.Returns(_indexes);
        await AcceptedInvitationIndexes.EnsureCreated(collection);
    }

    [Fact] void should_drop_the_legacy_index() => _indexes.Received(1).DropOneAsync("UniqueAcceptedInvitationSession", Arg.Any<CancellationToken>());
    [Fact] void should_create_a_unique_session_index_per_invitation() => _created.ShouldContain(model => model.Options.Name == "UniqueAcceptedInvitationSessionPerInvitation" && model.Options.Unique == true);
    [Fact] void should_not_recreate_the_legacy_index() => _created.ShouldNotContain(model => model.Options.Name == "UniqueAcceptedInvitationSession");
}
#endif
