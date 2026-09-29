// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AcceptedInvitationIndexRegistration.given;

namespace Ante.Invitations.Accepting.for_AcceptedInvitationIndexRegistration.when_classifying_a_failed_index_installation;

public class and_the_index_definition_conflicts : Specification
{
    readonly int[] _definitionConflicts = [85, 86, 11000];
    bool[] _retry = null!;

    void Because() => _retry = [.. _definitionConflicts.Select(code => AcceptedInvitationIndexRegistration.IsRetryable(
        an_index_error.Command(code)))];

    [Fact] void should_fail_for_each_conflict_code() => _retry.ShouldContainOnly([false, false, false]);
}
#endif
