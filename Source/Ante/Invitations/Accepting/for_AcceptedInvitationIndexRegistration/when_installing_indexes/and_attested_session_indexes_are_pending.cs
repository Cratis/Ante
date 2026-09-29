// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_AcceptedInvitationIndexRegistration.when_installing_indexes;

public class and_attested_session_indexes_are_pending : Specification
{
    readonly TaskCompletionSource<IEnumerable<string>> _sessionIndexesInstalled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ServiceProvider _provider = null!;
    AcceptedInvitationIndexRegistration _registration = null!;
    IMongoIndexManager<AcceptedInvitation> _legacyIndexes = null!;
    IMongoIndexManager<StagedInvitationTransaction> _stageIndexes = null!;
    IMongoIndexManager<AttestedInvitationSession> _sessionIndexes = null!;
    bool _readyBeforeCompletion;
    bool _readyAfterCompletion;

    void Establish()
    {
        var legacy = Substitute.For<IMongoCollection<AcceptedInvitation>>();
        _legacyIndexes = Substitute.For<IMongoIndexManager<AcceptedInvitation>>();
        legacy.Indexes.Returns(_legacyIndexes);
        _legacyIndexes.CreateManyAsync(Arg.Any<IEnumerable<CreateIndexModel<AcceptedInvitation>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>(["legacy"]));

        var stages = Substitute.For<IMongoCollection<StagedInvitationTransaction>>();
        _stageIndexes = Substitute.For<IMongoIndexManager<StagedInvitationTransaction>>();
        stages.Indexes.Returns(_stageIndexes);
        _stageIndexes.CreateOneAsync(Arg.Any<CreateIndexModel<StagedInvitationTransaction>>(), Arg.Any<CreateOneIndexOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("stage"));

        var sessions = Substitute.For<IMongoCollection<AttestedInvitationSession>>();
        _sessionIndexes = Substitute.For<IMongoIndexManager<AttestedInvitationSession>>();
        sessions.Indexes.Returns(_sessionIndexes);
        _sessionIndexes.CreateManyAsync(Arg.Any<IEnumerable<CreateIndexModel<AttestedInvitationSession>>>(), Arg.Any<CancellationToken>())
            .Returns(_sessionIndexesInstalled.Task);

        _provider = new ServiceCollection().AddSingleton(legacy).AddSingleton(stages).AddSingleton(sessions).BuildServiceProvider();
        _registration = new(
            _provider.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AcceptedInvitationIndexRegistration>.Instance,
            Options.Create(new InvitationExchangeConfig { Mode = InvitationExchangeMode.Attested }));
    }

    async Task Because()
    {
        await _registration.StartAsync(CancellationToken.None);
        _readyBeforeCompletion = _registration.IsReady;
        _sessionIndexesInstalled.SetResult(["session"]);
        await _registration.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));
        _readyAfterCompletion = _registration.IsReady;
    }

    async Task Destroy()
    {
        await _registration.StopAsync(CancellationToken.None);
        await _provider.DisposeAsync();
    }

    [Fact] async Task should_create_legacy_indexes() => await _legacyIndexes.Received(1).CreateManyAsync(Arg.Any<IEnumerable<CreateIndexModel<AcceptedInvitation>>>(), Arg.Any<CancellationToken>());
    [Fact] async Task should_create_stage_indexes() => await _stageIndexes.Received(1).CreateOneAsync(Arg.Any<CreateIndexModel<StagedInvitationTransaction>>(), Arg.Any<CreateOneIndexOptions>(), Arg.Any<CancellationToken>());
    [Fact] async Task should_create_session_indexes() => await _sessionIndexes.Received(1).CreateManyAsync(Arg.Any<IEnumerable<CreateIndexModel<AttestedInvitationSession>>>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_report_ready_before_the_last_index_succeeds() => _readyBeforeCompletion.ShouldBeFalse();
    [Fact] void should_report_ready_after_the_last_index_succeeds() => _readyAfterCompletion.ShouldBeTrue();
}
#endif
