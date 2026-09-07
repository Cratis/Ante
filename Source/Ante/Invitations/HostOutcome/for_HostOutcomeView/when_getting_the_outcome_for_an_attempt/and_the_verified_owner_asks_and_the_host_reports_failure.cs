// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.HostOutcome.for_HostOutcomeView.when_getting_the_outcome_for_an_attempt;

/// <summary>
/// A host-reported failure is visible to the verified owner exactly like a success - it never rewrites
/// or is confused with Ante's own durable publication, which has already succeeded by the time a host
/// outcome can even be requested.
/// </summary>
public class and_the_verified_owner_asks_and_the_host_reports_failure : Specification
{
    static readonly InvitationId _attemptId = InvitationId.New();

    ISignedInIdentity _signedInIdentity = null!;
    IHostOutcomeBackchannel _backchannel = null!;
    HostOutcomeView _result = null!;

    void Establish()
    {
        _backchannel = Substitute.For<IHostOutcomeBackchannel>();
        _backchannel.GetOutcome(_attemptId).Returns((HostOutcomeStatus.Failed, "quota-exceeded"));

        _signedInIdentity = Substitute.For<ISignedInIdentity>();
        _signedInIdentity.IsVerifiedOwnerOf(_attemptId).Returns(true);
    }

    async Task Because() =>
        _result = await HostOutcomeView.ForAttempt(_attemptId, _signedInIdentity, Options.Create(new AnteOptions { HostOutcomeUrl = "https://host.example.com/onboarding" }), _backchannel);

    [Fact] void should_be_failed() => Assert.Equal(HostOutcomeStatus.Failed, _result.Status);
    [Fact] void should_carry_the_reason_code() => Assert.Equal("quota-exceeded", _result.ReasonCode);
}
#endif
