// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.OrganizationSetup.for_OrganizationPublicationStatusNotifier.given;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationPublicationStatusNotifier.when_recording;

/// <summary>
/// Only the acceptance that names the organization says setup was recorded; a legal fact alone does not.
/// </summary>
public class and_the_legal_acceptance_is_recorded : an_organization_notification
{
    readonly List<OrganizationSetupAcceptanceStatusView> _observed = [];

    void Establish() => Subscriptions.GetStatus(Id).Subscribe(_observed.Add);

    async Task Because() => await Notifier.NotifyRecorded((EventSourceId)Id.Value.ToString("D"), Legal().Content);

    [Fact] void should_leave_the_subscription_pending() => Assert.Equal([OrganizationSetupAcceptanceStatus.Pending], _observed.Select(view => view.Status));
}
#endif
