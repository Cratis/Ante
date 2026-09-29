// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AcceptedInvitationIndexRegistration.given;

namespace Ante.Invitations.Accepting.for_AcceptedInvitationIndexRegistration.when_classifying_a_failed_index_installation;

public class and_the_server_is_incompatible_with_the_driver : Specification
{
    bool _retry;
    bool _misconfigured;

    void Because()
    {
        var error = an_index_error.IncompatibleDriver();
        _retry = AcceptedInvitationIndexRegistration.IsRetryable(error);
        _misconfigured = AcceptedInvitationIndexRegistration.IsMisconfigured(error);
    }

    [Fact] void should_not_retry() => _retry.ShouldBeFalse();
    [Fact] void should_be_reported_as_misconfigured() => _misconfigured.ShouldBeTrue();
}
#endif
