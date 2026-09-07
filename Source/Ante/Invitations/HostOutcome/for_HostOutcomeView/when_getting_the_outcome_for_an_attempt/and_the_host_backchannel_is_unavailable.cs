// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.HostOutcome.for_HostOutcomeView.when_getting_the_outcome_for_an_attempt;

/// <summary>
/// An unreachable host degrades to the same safe default a non-owner sees - never an exception, and
/// never anything that blocks the wizard from continuing. <see cref="HostOutcomeBackchannel"/> already
/// guarantees it never throws past this boundary; this proves the query surfaces that fail-safe answer
/// unchanged rather than reinterpreting it.
/// </summary>
public class and_the_host_backchannel_is_unavailable : Specification
{
    static readonly InvitationId _attemptId = InvitationId.New();

    ISignedInIdentity _signedInIdentity = null!;
    IHostOutcomeBackchannel _backchannel = null!;
    HostOutcomeView _result = null!;

    void Establish()
    {
        _backchannel = Substitute.For<IHostOutcomeBackchannel>();
        _backchannel.GetOutcome(_attemptId).Returns((HostOutcomeStatus.Unknown, string.Empty));

        _signedInIdentity = Substitute.For<ISignedInIdentity>();
        _signedInIdentity.IsVerifiedOwnerOf(_attemptId).Returns(true);
    }

    async Task Because() =>
        _result = await HostOutcomeView.ForAttempt(_attemptId, _signedInIdentity, Options.Create(new AnteOptions { HostOutcomeUrl = "https://host.example.com/onboarding" }), _backchannel);

    [Fact] void should_report_configured() => Assert.True(_result.IsConfigured);
    [Fact] void should_be_unknown() => Assert.Equal(HostOutcomeStatus.Unknown, _result.Status);
    [Fact] void should_not_throw() => Assert.NotNull(_result);
}
#endif
