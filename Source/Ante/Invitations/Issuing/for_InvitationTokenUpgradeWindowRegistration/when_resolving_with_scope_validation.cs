// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;
using MongoDB.Driver;

namespace Ante.Invitations.Issuing.for_InvitationTokenUpgradeWindowRegistration;

public class when_resolving_with_scope_validation : Specification
{
    IInvitationTokenUpgradeWindow? _window;
    Exception? _error;

    void Because()
    {
        var activations = Substitute.For<IMongoCollection<InvitationTokenIsolationActivation>>();
        activations.FindOneAndUpdateAsync(
            Arg.Any<FilterDefinition<InvitationTokenIsolationActivation>>(),
            Arg.Any<UpdateDefinition<InvitationTokenIsolationActivation>>(),
            Arg.Any<FindOneAndUpdateOptions<InvitationTokenIsolationActivation, InvitationTokenIsolationActivation>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new InvitationTokenIsolationActivation(InvitationTokenIsolationActivation.Singleton, DateTimeOffset.UtcNow)));
        var database = Substitute.For<IMongoDatabase>();
        database.GetCollection<InvitationTokenIsolationActivation>("invitation-token-isolation", Arg.Any<MongoCollectionSettings>()).Returns(activations);
        var accepted = Substitute.For<IMongoCollection<AcceptedInvitation>>();
        accepted.Database.Returns(database);

        // Like Development: the collection is scoped, and resolving it from the root provider throws.
        var provider = new ServiceCollection()
            .AddScoped(_ => accepted)
            .AddInvitationTokenUpgradeWindow(TimeSpan.FromDays(7))
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        try
        {
            _window = provider.GetRequiredService<IInvitationTokenUpgradeWindow>();
        }
        catch (Exception error)
        {
            _error = error;
        }
    }

    [Fact] void should_not_fail_scope_validation() => Assert.Null(_error);
    [Fact] void should_provide_the_loaded_window() => Assert.NotNull(_window);
}
#endif
