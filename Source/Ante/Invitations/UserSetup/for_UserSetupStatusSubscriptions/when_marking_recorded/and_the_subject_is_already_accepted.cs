// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.UserSetup.for_UserSetupStatusSubscriptions.when_marking_recorded;

/// <summary>
/// A recorded push that arrives after the subject is accepted - a redelivered acceptance event, or a stale
/// seeding read - must be ignored: accepted is terminal and a watching client must never see it undone.
/// </summary>
public class and_the_subject_is_already_accepted : Specification
{
    readonly InvitationId _id = InvitationId.New();
    readonly List<UserSetupAcceptanceStatus> _observed = [];
    UserSetupStatusSubscriptions _subscriptions = null!;

    void Establish()
    {
        _subscriptions = new();
        _subscriptions.GetStatus(_id).Subscribe(view => _observed.Add(view.Status));
        _subscriptions.MarkAccepted(_id);
    }

    void Because()
    {
        _subscriptions.MarkRecorded(_id);
        _subscriptions.MarkRecordedIfWatched(_id);
    }

    void Destroy() => _subscriptions.Dispose();

    [Fact] void should_not_have_emitted_recorded() => Assert.Equal([UserSetupAcceptanceStatus.Pending, UserSetupAcceptanceStatus.Accepted], _observed);
}
#endif
