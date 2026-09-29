// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.UserSetup.for_UserSetupStatusSubscriptions.when_marking_recorded;

/// <summary>
/// With no subscription open there is nobody to tell, and a subscription that opens later is seeded from
/// durable facts - so nothing may be created for an acceptance nobody watches, or a replay of the reactor
/// would leave an entry behind for every acceptance ever recorded.
/// </summary>
public class and_nobody_is_watching : Specification
{
    readonly InvitationId _id = InvitationId.New();
    UserSetupStatusSubscriptions _subscriptions = null!;
    UserSetupAcceptanceStatus _status;

    void Establish() => _subscriptions = new();

    void Because()
    {
        _subscriptions.MarkRecordedIfWatched(_id);
        _status = ((BehaviorSubject<UserSetupAcceptanceStatusView>)_subscriptions.GetStatus(_id)).Value.Status;
    }

    void Destroy() => _subscriptions.Dispose();

    [Fact] void should_start_a_later_subscription_from_pending() => Assert.Equal(UserSetupAcceptanceStatus.Pending, _status);
}
#endif
