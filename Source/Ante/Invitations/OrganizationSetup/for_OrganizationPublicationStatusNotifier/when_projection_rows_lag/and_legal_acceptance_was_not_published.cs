// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.OrganizationSetup.for_OrganizationPublicationStatusNotifier.given;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationPublicationStatusNotifier.when_projection_rows_lag;

public class and_legal_acceptance_was_not_published : an_organization_notification
{
    void Establish()
    {
        Local.AddRange([Invited(), Legal()]);
        Outbox.Add(Invited());
    }

    async Task Because() => await Notifier.NotifyIfPublished(Id.Value.ToString("D"));

    [Fact] void should_keep_status_pending() => Status.Status.ShouldEqual(OrganizationSetupAcceptanceStatus.Pending);
}
#endif
