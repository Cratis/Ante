// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.HostOutcome.for_HostOutcomeView.when_getting_the_outcome_for_an_attempt;

public class and_the_caller_is_anonymous : Specification
{
    readonly InvitationId _id = InvitationId.New();
    IHostOutcomeBackchannel _backchannel = null!;
    HostOutcomeView _result = null!;

    async Task Because()
    {
        _backchannel = Substitute.For<IHostOutcomeBackchannel>();
        _result = await HostOutcomeView.ForAttempt(
            _id,
            Substitute.For<ISignedInIdentity>(),
            Options.Create(new AnteOptions { HostOutcomeUrl = "https://host.example.com/onboarding" }),
            _backchannel);
    }

    [Fact] void should_report_unknown() => Assert.Equal(HostOutcomeStatus.Unknown, _result.Status);
    [Fact] void should_not_reveal_an_outcome() => Assert.Equal(string.Empty, _result.ReasonCode);
    [Fact] void should_not_contact_the_host() => _backchannel.DidNotReceiveWithAnyArgs().GetOutcome(default!);
}
#endif
