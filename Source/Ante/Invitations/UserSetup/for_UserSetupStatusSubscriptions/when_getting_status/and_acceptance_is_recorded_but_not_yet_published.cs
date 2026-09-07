// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.UserSetup.for_UserSetupStatusSubscriptions.when_getting_status;

/// <summary>
/// A client that reconnects after submitting - but before publication reached the outbox - must see
/// Recorded, not Pending: Pending would tell the wizard it is safe to resubmit, which would collide with
/// the one-use invitation constraint.
/// </summary>
public class and_acceptance_is_recorded_but_not_yet_published : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    UserSetupStatusSubscriptions _subscriptions = null!;
    UserSetupAcceptanceStatusView _result = null!;

    void Establish() => _subscriptions = new();

    void Because() => _result = ((BehaviorSubject<UserSetupAcceptanceStatusView>)_subscriptions.GetStatus(_invitationId, isRecorded: true, isFullyPublished: false)).Value;

    [Fact] void should_be_recorded() => Assert.Equal(UserSetupAcceptanceStatus.Recorded, _result.Status);
}
#endif
