// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AcceptedInvitationIndexRegistration.given;

namespace Ante.Invitations.Accepting.for_AcceptedInvitationIndexRegistration.when_classifying_a_failed_index_installation;

public class and_the_command_error_has_a_transient_code : Specification
{
    readonly int[] _transientCodes = [6, 7, 89, 91, 189, 262, 9001, 10107, 11600, 11602, 13435, 13436];
    bool[] _retry = null!;

    void Because() => _retry = [.. _transientCodes.Select(code => AcceptedInvitationIndexRegistration.IsRetryable(
        an_index_error.Command(code)))];

    [Fact] void should_retry_for_each_transient_code() => _retry.ShouldContainOnly(Enumerable.Repeat(true, _transientCodes.Length));
}
#endif
