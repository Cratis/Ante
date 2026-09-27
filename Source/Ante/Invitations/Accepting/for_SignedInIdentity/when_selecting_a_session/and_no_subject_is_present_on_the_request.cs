// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_selecting_a_session;

public class and_no_subject_is_present_on_the_request : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();
    static readonly AcceptedInvitation _session = new(
        "some-subject",
        "github",
        _invitationId,
        InvitationFlowType.JoinTenant,
        DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow.AddMinutes(30));

    AcceptedInvitation? _result;

    // No forwarded subject means there is no evidence this request owns any exchange session.
    void Because() =>
        _result = SignedInIdentity.SelectSession([_session], _invitationId, null, "github", DateTimeOffset.UtcNow);

    [Fact] void should_not_select_any_session() => Assert.Null(_result);
}
#endif
