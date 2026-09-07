// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.HostOutcome.for_HostOutcomeView.when_getting_the_outcome_for_an_attempt;

/// <summary>
/// With no <see cref="AnteOptions.HostOutcomeUrl"/> configured, every wizard must behave exactly as it
/// does today - nothing is looked up, and the backchannel is never even asked, regardless of who is
/// asking.
/// </summary>
public class and_the_adapter_is_not_configured : Specification
{
    static readonly InvitationId _attemptId = InvitationId.New();

    ISignedInIdentity _signedInIdentity = null!;
    IHostOutcomeBackchannel _backchannel = null!;
    HostOutcomeView _result = null!;

    void Establish()
    {
        _backchannel = Substitute.For<IHostOutcomeBackchannel>();

        _signedInIdentity = Substitute.For<ISignedInIdentity>();
        _signedInIdentity.IsVerifiedOwnerOf(_attemptId).Returns(true);
    }

    async Task Because() =>
        _result = await HostOutcomeView.ForAttempt(_attemptId, _signedInIdentity, Options.Create(new AnteOptions { HostOutcomeUrl = string.Empty }), _backchannel);

    [Fact] void should_report_not_configured() => Assert.False(_result.IsConfigured);
    [Fact] void should_be_unknown() => Assert.Equal(HostOutcomeStatus.Unknown, _result.Status);
    [Fact] void should_never_ask_the_backchannel() => _backchannel.DidNotReceiveWithAnyArgs().GetOutcome(default!);
}
#endif
