// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.OrganizationSetup.for_OrganizationPublicationStatusNotifier.given;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationPublicationStatusNotifier.when_recording;

public class and_an_invited_organization_is_recorded : an_organization_notification
{
    readonly List<OrganizationSetupAcceptanceStatusView> _observed = [];

    void Establish() => Subscriptions.GetStatus(Id).Subscribe(_observed.Add);

    async Task Because() => await Notifier.NotifyRecorded((EventSourceId)Id.Value.ToString("D"), Invited().Content);

    [Fact] void should_push_recorded_to_the_open_subscription() => Assert.Equal([OrganizationSetupAcceptanceStatus.Pending, OrganizationSetupAcceptanceStatus.Recorded], _observed.Select(view => view.Status));
    [Fact] void should_carry_the_organization_name() => Assert.Equal("Invited Org", (string)_observed[^1].OrganizationName);
}
#endif
