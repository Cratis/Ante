// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.UserSetup.for_UserSetupStatusSubscriptions.when_marking_recorded;

/// <summary>
/// A redelivered or replayed acceptance - or a re-seeding read - marks a subject that is already recorded
/// again. Subscribers already know, so nothing more is emitted.
/// </summary>
public class and_the_subject_is_already_recorded : Specification
{
    readonly InvitationId _id = InvitationId.New();
    readonly List<UserSetupAcceptanceStatus> _observed = [];
    UserSetupStatusSubscriptions _subscriptions = null!;

    void Establish()
    {
        _subscriptions = new();
        _subscriptions.GetStatus(_id).Subscribe(view => _observed.Add(view.Status));
        _subscriptions.MarkRecorded(_id);
    }

    void Because()
    {
        _subscriptions.MarkRecorded(_id);
        _subscriptions.MarkRecordedIfWatched(_id);
    }

    void Destroy() => _subscriptions.Dispose();

    [Fact] void should_have_emitted_recorded_only_once() => Assert.Equal([UserSetupAcceptanceStatus.Pending, UserSetupAcceptanceStatus.Recorded], _observed);
}
#endif
