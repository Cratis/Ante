// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_selecting_a_session;

public class and_the_subject_matches_a_live_session_for_the_invitation : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();
    static readonly AcceptedInvitation _session = new(
        "requesting-subject",
        "github",
        _invitationId,
        InvitationFlowType.JoinTenant,
        DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow.AddMinutes(30));

    AcceptedInvitation? _result;

    void Because() =>
        _result = SignedInIdentity.SelectSession([_session], _invitationId, "requesting-subject", DateTimeOffset.UtcNow);

    [Fact] void should_select_the_session() => Assert.Same(_session, _result);
}
#endif
