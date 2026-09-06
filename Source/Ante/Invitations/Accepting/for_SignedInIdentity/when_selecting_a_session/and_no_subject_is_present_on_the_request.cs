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

    // No subject to disambiguate with - the most recent session for the invitation is the best guess
    // available. This fallback exists only for resolving which login a request belongs to, never for
    // deciding ownership - IsVerifiedOwnerOf never reaches it (see for_SignedInIdentity/when_verifying_ownership).
    void Because() =>
        _result = SignedInIdentity.SelectSession([_session], _invitationId, null, DateTimeOffset.UtcNow);

    [Fact] void should_select_the_only_session() => Assert.Same(_session, _result);
}
#endif
