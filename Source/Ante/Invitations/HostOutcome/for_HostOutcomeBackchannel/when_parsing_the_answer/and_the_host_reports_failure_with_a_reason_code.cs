// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.HostOutcome.for_HostOutcomeBackchannel.when_parsing_the_answer;

public class and_the_host_reports_failure_with_a_reason_code : Specification
{
    (HostOutcomeStatus Status, string ReasonCode) _result;

    void Because() => _result = HostOutcomeBackchannel.ParseAnswer(new HostOutcomeAnswer("failed", "quota-exceeded"));

    [Fact] void should_be_failed() => Assert.Equal(HostOutcomeStatus.Failed, _result.Status);
    [Fact] void should_carry_the_reason_code() => Assert.Equal("quota-exceeded", _result.ReasonCode);
}
#endif
