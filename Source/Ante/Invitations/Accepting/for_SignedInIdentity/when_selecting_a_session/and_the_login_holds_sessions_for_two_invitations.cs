// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_selecting_a_session;

// Each invitation the login has exchanged keeps its own session, so the earlier one is still owned.
public class and_the_login_holds_sessions_for_two_invitations : Specification
{
    readonly InvitationId _earlier = Guid.NewGuid();
    readonly InvitationId _later = Guid.NewGuid();
    AcceptedInvitation? _earlierResult;
    AcceptedInvitation? _laterResult;

    void Because()
    {
        var now = DateTimeOffset.UtcNow;
        AcceptedInvitation[] sessions =
        [
            new("subject", "github", _earlier, InvitationFlowType.JoinTenant, now.AddMinutes(-10), now.AddDays(1)),
            new("subject", "github", _later, InvitationFlowType.JoinTenant, now, now.AddDays(1)),
        ];
        _earlierResult = SignedInIdentity.SelectSession(sessions, _earlier, "subject", "github", now);
        _laterResult = SignedInIdentity.SelectSession(sessions, _later, "subject", "github", now);
    }

    [Fact] void should_still_own_the_earlier_invitation() => _earlierResult!.InvitationId.ShouldEqual(_earlier);
    [Fact] void should_own_the_later_invitation() => _laterResult!.InvitationId.ShouldEqual(_later);
}
#endif
