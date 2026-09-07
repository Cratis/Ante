// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.HostOutcome.for_HostOutcomeBackchannel.when_parsing_the_answer;

/// <summary>
/// A host reporting a status Ante does not recognize - a typo, a future value an older Ante build has
/// never seen - must degrade to the same safe default as an unreachable host, never throw or surface the
/// raw value.
/// </summary>
public class and_the_status_is_not_recognized : Specification
{
    (HostOutcomeStatus Status, string ReasonCode) _result;

    void Because() => _result = HostOutcomeBackchannel.ParseAnswer(new HostOutcomeAnswer("something-unexpected", "whatever"));

    [Fact] void should_be_unknown() => Assert.Equal(HostOutcomeStatus.Unknown, _result.Status);
    [Fact] void should_carry_no_reason_code() => Assert.Equal(string.Empty, _result.ReasonCode);
}
#endif
