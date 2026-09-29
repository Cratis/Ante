// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.UserSetup.for_JoinTenantPublicationStatusNotifier.given;

namespace Ante.Invitations.UserSetup.for_JoinTenantPublicationStatusNotifier.when_recording;

/// <summary>
/// The notifier only speaks for the join-tenant flow; the same id carrying another flow's acceptance is not
/// its business, and must not move its subscription.
/// </summary>
public class and_another_flows_acceptance_is_recorded : a_join_notification
{
    readonly List<UserSetupAcceptanceStatus> _observed = [];

    void Establish() => Subscriptions.GetStatus(Id).Subscribe(view => _observed.Add(view.Status));

    async Task Because() => await Notifier.NotifyRecorded((EventSourceId)Id.Value.ToString("D"), Legal().Content);

    [Fact] void should_leave_the_subscription_pending() => Assert.Equal([UserSetupAcceptanceStatus.Pending], _observed);
}
#endif
