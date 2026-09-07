// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.HostOutcome.for_HostOutcomeView.when_getting_the_outcome_for_an_attempt;

/// <summary>
/// A caller who is not a verified owner of the attempt - a stranger who merely knows the attempt id, or
/// is asking about an attempt that never existed - must never learn anything about it, even when the
/// host outcome adapter is configured and the host itself would have answered with a real, terminal
/// result. The query never even asks the backchannel in this case, so nothing about a real result can
/// leak through timing or a substituted answer either.
/// </summary>
public class and_the_caller_is_not_a_verified_owner : Specification
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
        _signedInIdentity.IsVerifiedOwnerOf(_attemptId).Returns(false);
    }

    async Task Because() =>
        _result = await HostOutcomeView.ForAttempt(_attemptId, _signedInIdentity, Options.Create(new AnteOptions { HostOutcomeUrl = "https://host.example.com/onboarding" }), _backchannel);

    [Fact] void should_still_report_configured() => Assert.True(_result.IsConfigured);
    [Fact] void should_be_unknown_rather_than_the_hosts_real_answer() => Assert.Equal(HostOutcomeStatus.Unknown, _result.Status);
    [Fact] void should_carry_no_reason_code() => Assert.Equal(string.Empty, _result.ReasonCode);
    [Fact] void should_never_ask_the_backchannel() => _backchannel.DidNotReceiveWithAnyArgs().GetOutcome(default!);
}
#endif
