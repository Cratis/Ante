// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.HostOutcome.for_HostOutcomeView.when_getting_the_outcome_for_an_attempt;

public class and_the_verified_owner_asks_and_the_host_reports_success : Specification
{
    static readonly InvitationId _attemptId = InvitationId.New();

    ISignedInIdentity _signedInIdentity = null!;
    IHostOutcomeBackchannel _backchannel = null!;
    HostOutcomeView _result = null!;

    void Establish()
    {
        _backchannel = Substitute.For<IHostOutcomeBackchannel>();
        _backchannel.GetOutcome(_attemptId).Returns((HostOutcomeStatus.Succeeded, "tenant-provisioned"));

        _signedInIdentity = Substitute.For<ISignedInIdentity>();
        _signedInIdentity.IsVerifiedOwnerOf(_attemptId).Returns(true);
    }

    async Task Because() =>
        _result = await HostOutcomeView.ForAttempt(_attemptId, _signedInIdentity, Options.Create(new AnteOptions { HostOutcomeUrl = "https://host.example.com/onboarding" }), _backchannel);

    [Fact] void should_report_configured() => Assert.True(_result.IsConfigured);
    [Fact] void should_carry_the_attempt_id() => Assert.Equal(_attemptId, _result.AttemptId);
    [Fact] void should_be_succeeded() => Assert.Equal(HostOutcomeStatus.Succeeded, _result.Status);
    [Fact] void should_carry_the_reason_code() => Assert.Equal("tenant-provisioned", _result.ReasonCode);
}
#endif
