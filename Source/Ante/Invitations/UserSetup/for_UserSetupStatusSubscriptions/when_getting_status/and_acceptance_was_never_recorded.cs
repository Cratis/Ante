// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.UserSetup.for_UserSetupStatusSubscriptions.when_getting_status;

/// <summary>
/// A client that has never submitted anything must see Pending - the state that tells the wizard it is
/// safe to render the form.
/// </summary>
public class and_acceptance_was_never_recorded : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    UserSetupStatusSubscriptions _subscriptions = null!;
    UserSetupAcceptanceStatusView _result = null!;

    void Establish() => _subscriptions = new();

    void Because() => _result = ((BehaviorSubject<UserSetupAcceptanceStatusView>)_subscriptions.GetStatus(_invitationId)).Value;

    [Fact] void should_be_pending() => Assert.Equal(UserSetupAcceptanceStatus.Pending, _result.Status);
}
#endif
