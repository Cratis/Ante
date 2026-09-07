// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.UserSetup.for_UserSetupStatusSubscriptions.when_getting_status;

/// <summary>
/// A client that reconnects once every required fact has reached the outbox must see Accepted - the only
/// state safe to hand off to the host - regardless of whether this process handled the original command.
/// </summary>
public class and_acceptance_is_fully_published : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    UserSetupStatusSubscriptions _subscriptions = null!;
    UserSetupAcceptanceStatusView _result = null!;

    void Establish() => _subscriptions = new();

    void Because() => _result = ((BehaviorSubject<UserSetupAcceptanceStatusView>)_subscriptions.GetStatus(_invitationId, isRecorded: true, isFullyPublished: true)).Value;

    [Fact] void should_be_accepted() => Assert.Equal(UserSetupAcceptanceStatus.Accepted, _result.Status);
}
#endif
