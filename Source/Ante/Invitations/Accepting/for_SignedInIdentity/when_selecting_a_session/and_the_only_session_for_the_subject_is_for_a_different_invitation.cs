// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_selecting_a_session;

public class and_the_only_session_for_the_subject_is_for_a_different_invitation : Specification
{
    static readonly InvitationId _targetInvitationId = InvitationId.New();
    static readonly InvitationId _otherInvitationId = InvitationId.New();
    static readonly AcceptedInvitation _session = new(
        "requesting-subject",
        "github",
        _otherInvitationId,
        InvitationFlowType.JoinTenant,
        DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow.AddMinutes(30));

    AcceptedInvitation? _result;

    void Because() =>
        _result = SignedInIdentity.SelectSession([_session], _targetInvitationId, "requesting-subject", DateTimeOffset.UtcNow);

    [Fact] void should_select_no_session() => Assert.Null(_result);
}
#endif
