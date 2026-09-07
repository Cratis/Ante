// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.HostOutcome.for_HostOutcomeBackchannel.when_parsing_the_answer;

/// <summary>
/// A response body that deserializes to nothing - an empty body, or a host that answered 204 - must
/// degrade to the same safe default as an unreachable host, never throw.
/// </summary>
public class and_the_answer_is_missing : Specification
{
    (HostOutcomeStatus Status, string ReasonCode) _result;

    void Because() => _result = HostOutcomeBackchannel.ParseAnswer(null);

    [Fact] void should_be_unknown() => Assert.Equal(HostOutcomeStatus.Unknown, _result.Status);
    [Fact] void should_carry_no_reason_code() => Assert.Equal(string.Empty, _result.ReasonCode);
}
#endif
