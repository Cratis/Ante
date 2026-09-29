// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupStatusSubscriptions.when_marking_recorded;

/// <summary>
/// A redelivered or replayed acceptance - or a re-seeding read - marks a subject that is already recorded
/// again. Subscribers already know, so nothing more is emitted.
/// </summary>
public class and_the_subject_is_already_recorded : Specification
{
    readonly InvitationId _id = InvitationId.New();
    readonly List<OrganizationSetupAcceptanceStatus> _observed = [];
    OrganizationSetupStatusSubscriptions _subscriptions = null!;

    void Establish()
    {
        _subscriptions = new();
        _subscriptions.GetStatus(_id).Subscribe(view => _observed.Add(view.Status));
        _subscriptions.MarkRecorded(_id, "Acme");
    }

    void Because()
    {
        _subscriptions.MarkRecorded(_id, "Acme");
        _subscriptions.MarkRecordedIfWatched(_id, "Acme");
    }

    void Destroy() => _subscriptions.Dispose();

    [Fact] void should_have_emitted_recorded_only_once() => Assert.Equal([OrganizationSetupAcceptanceStatus.Pending, OrganizationSetupAcceptanceStatus.Recorded], _observed);
}
#endif
