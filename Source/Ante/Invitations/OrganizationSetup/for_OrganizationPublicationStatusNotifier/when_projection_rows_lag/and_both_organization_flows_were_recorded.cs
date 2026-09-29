// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.OrganizationSetup.for_OrganizationPublicationStatusNotifier.given;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationPublicationStatusNotifier.when_projection_rows_lag;

/// <summary>
/// An invitation id and a registration id sharing one event source is ambiguous: which organization name
/// the status carries cannot be decided from the log, so nothing may unlock the stream.
/// </summary>
public class and_both_organization_flows_were_recorded : an_organization_notification
{
    void Establish()
    {
        Local.AddRange([Invited(), Registered()]);
        Outbox.AddRange([Invited(), Registered()]);
    }

    async Task Because() => await Notifier.NotifyIfPublished(Id.Value.ToString("D"));

    [Fact] void should_keep_status_pending() => Status.Status.ShouldEqual(OrganizationSetupAcceptanceStatus.Pending);
}
#endif
