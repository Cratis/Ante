// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AcceptedInvitationIndexRegistration.given;

namespace Ante.Invitations.Accepting.for_AcceptedInvitationIndexRegistration.when_classifying_a_failed_index_installation;

public class and_server_selection_times_out : Specification
{
    bool _retry;

    void Because() => _retry = AcceptedInvitationIndexRegistration.IsRetryable(new TimeoutException());

    [Fact] void should_retry() => _retry.ShouldBeTrue();
}
#endif
