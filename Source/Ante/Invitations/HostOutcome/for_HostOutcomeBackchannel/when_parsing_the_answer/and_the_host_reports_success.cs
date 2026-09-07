// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.HostOutcome.for_HostOutcomeBackchannel.when_parsing_the_answer;

public class and_the_host_reports_success : Specification
{
    (HostOutcomeStatus Status, string ReasonCode) _result;

    void Because() => _result = HostOutcomeBackchannel.ParseAnswer(new HostOutcomeAnswer("succeeded", "tenant-provisioned"));

    [Fact] void should_be_succeeded() => Assert.Equal(HostOutcomeStatus.Succeeded, _result.Status);
    [Fact] void should_carry_the_reason_code() => Assert.Equal("tenant-provisioned", _result.ReasonCode);
}
#endif
