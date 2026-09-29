// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupStatusSubscriptions.when_marking_recorded;

/// <summary>
/// A subscription that is already open on this replica learns straight away that the acceptance is recorded.
/// </summary>
public class and_a_subscription_is_open : Specification
{
    readonly InvitationId _id = InvitationId.New();
    readonly List<OrganizationSetupAcceptanceStatus> _observed = [];
    OrganizationSetupStatusSubscriptions _subscriptions = null!;

    void Establish()
    {
        _subscriptions = new();
        _subscriptions.GetStatus(_id).Subscribe(view => _observed.Add(view.Status));
    }

    void Because() => _subscriptions.MarkRecordedIfWatched(_id, "Acme");

    void Destroy() => _subscriptions.Dispose();

    [Fact] void should_move_from_pending_to_recorded() => Assert.Equal([OrganizationSetupAcceptanceStatus.Pending, OrganizationSetupAcceptanceStatus.Recorded], _observed);
}
#endif
