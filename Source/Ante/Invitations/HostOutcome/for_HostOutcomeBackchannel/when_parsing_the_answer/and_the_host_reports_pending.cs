// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.HostOutcome.for_HostOutcomeBackchannel.when_parsing_the_answer;

public class and_the_host_reports_pending : Specification
{
    (HostOutcomeStatus Status, string ReasonCode) _result;

    void Because() => _result = HostOutcomeBackchannel.ParseAnswer(new HostOutcomeAnswer("pending", null));

    [Fact] void should_be_pending() => Assert.Equal(HostOutcomeStatus.Pending, _result.Status);
    [Fact] void should_carry_no_reason_code() => Assert.Equal(string.Empty, _result.ReasonCode);
}
#endif
