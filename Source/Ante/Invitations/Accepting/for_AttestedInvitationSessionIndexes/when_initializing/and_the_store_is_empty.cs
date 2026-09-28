// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_AttestedInvitationSessionIndexes.when_initializing;

public class and_the_store_is_empty : Specification
{
    CreateIndexModel<AttestedInvitationSession>[] _indexes = [];

    async Task Because()
    {
        var sessions = Substitute.For<IMongoCollection<AttestedInvitationSession>>();
        var indexManager = Substitute.For<IMongoIndexManager<AttestedInvitationSession>>();
        sessions.Indexes.Returns(indexManager);
        indexManager.CreateManyAsync(Arg.Do<IEnumerable<CreateIndexModel<AttestedInvitationSession>>>(models => _indexes = [.. models]),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult<IEnumerable<string>>([]));
        await AttestedInvitationSessionIndexes.EnsureCreated(sessions);
    }

    [Fact] void should_make_transaction_and_actor_and_assertion_unique() => Assert.Equal(
        ["UniqueAttestedActor", "UniqueCompletionAssertion", "UniqueCompletionTransaction"],
        _indexes.Where(index => index.Options.Unique == true).Select(index => index.Options.Name));
    [Fact] void should_install_expiry_cleanup() => Assert.Equal(TimeSpan.Zero,
        _indexes.Single(index => index.Options.Name == "AttestedSessionExpiry").Options.ExpireAfter);
}
#endif
