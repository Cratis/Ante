// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.HostOutcome.for_HostOutcomeView.when_getting_the_outcome_for_an_attempt;

public class and_another_actor_becomes_owner_during_the_backchannel_call : Specification
{
    static readonly InvitationId _attemptId = InvitationId.New();

    ISignedInIdentity _signedInIdentity = null!;
    IHostOutcomeBackchannel _backchannel = null!;
    IEventStore _eventStore = null!;
    HostOutcomeView _result = null!;
    bool _isOwner = true;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _signedInIdentity = Substitute.For<ISignedInIdentity>();
        _signedInIdentity.IsVerifiedRecoveryOwnerOf(_attemptId, _eventStore).Returns(_ => _isOwner);

        _backchannel = Substitute.For<IHostOutcomeBackchannel>();
        _backchannel.GetOutcome(_attemptId).Returns(_ =>
        {
            _isOwner = false;
            return Task.FromResult((HostOutcomeStatus.Succeeded, "tenant-provisioned"));
        });
    }

    async Task Because() =>
        _result = await HostOutcomeView.ForAttempt(_attemptId, _signedInIdentity, Options.Create(new AnteOptions { HostOutcomeUrl = "https://host.example.com/onboarding" }), _backchannel, _eventStore);

    [Fact] void should_be_unknown() => Assert.Equal(HostOutcomeStatus.Unknown, _result.Status);
    [Fact] void should_not_disclose_the_hosts_reason() => Assert.Equal(string.Empty, _result.ReasonCode);
    [Fact] void should_recheck_the_committed_owner_after_the_call() => _signedInIdentity.Received(2).IsVerifiedRecoveryOwnerOf(_attemptId, _eventStore);
}
#endif
