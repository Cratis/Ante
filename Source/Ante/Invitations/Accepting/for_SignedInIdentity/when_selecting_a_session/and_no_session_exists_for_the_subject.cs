// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_selecting_a_session;

public class and_no_session_exists_for_the_subject : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    AcceptedInvitation? _result;

    void Because() =>
        _result = SignedInIdentity.SelectSession([], _invitationId, "requesting-subject", DateTimeOffset.UtcNow);

    [Fact] void should_select_no_session() => Assert.Null(_result);
}
#endif
