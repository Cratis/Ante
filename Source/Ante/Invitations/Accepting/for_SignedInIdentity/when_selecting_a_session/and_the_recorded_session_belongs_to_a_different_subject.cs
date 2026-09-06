// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_selecting_a_session;

public class and_the_recorded_session_belongs_to_a_different_subject : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();
    static readonly AcceptedInvitation _session = new(
        "someone-elses-subject",
        "github",
        _invitationId,
        InvitationFlowType.JoinTenant,
        DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow.AddMinutes(30));

    AcceptedInvitation? _result;

    // The request names a subject that never exchanged this invitation - a different login recorded
    // the only session there is for it. This is contradictory evidence, not missing evidence, and
    // must be rejected outright rather than "best guess" borrowing that other login's session.
    void Because() =>
        _result = SignedInIdentity.SelectSession([_session], _invitationId, "requesting-subject", DateTimeOffset.UtcNow);

    [Fact] void should_select_no_session() => Assert.Null(_result);
}
#endif
