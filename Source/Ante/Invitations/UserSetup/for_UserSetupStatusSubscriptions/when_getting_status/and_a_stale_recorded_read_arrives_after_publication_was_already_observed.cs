// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.UserSetup.for_UserSetupStatusSubscriptions.when_getting_status;

/// <summary>
/// Two reconnects can race a lagging read: one reads the outbox collection and observes full publication
/// first, the other reads only the (equally durable, but no longer relevant) recorded collection and
/// reports isFullyPublished: false. The second call must never regress a subject another tab is already
/// watching from Accepted back down to Recorded - durable evidence only ever moves the subject forward.
/// </summary>
public class and_a_stale_recorded_read_arrives_after_publication_was_already_observed : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    UserSetupStatusSubscriptions _subscriptions = null!;
    UserSetupAcceptanceStatusView _result = null!;

    void Establish()
    {
        _subscriptions = new();
        _subscriptions.GetStatus(_invitationId, isRecorded: true, isFullyPublished: true);
    }

    void Because() => _result = ((BehaviorSubject<UserSetupAcceptanceStatusView>)_subscriptions.GetStatus(_invitationId, isRecorded: true, isFullyPublished: false)).Value;

    [Fact] void should_still_be_accepted() => Assert.Equal(UserSetupAcceptanceStatus.Accepted, _result.Status);
}
#endif
