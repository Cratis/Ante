// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.OrganizationSetup.for_OrganizationPublicationStatusNotifier.given;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationPublicationStatusNotifier.when_projection_rows_lag;

public class and_self_registration_was_published : an_organization_notification
{
    void Establish()
    {
        Local.Add(Registered());
        Outbox.Add(Registered());
    }

    async Task Because() => await Notifier.NotifyIfPublished(Id.Value.ToString("D"));

    [Fact] void should_mark_status_accepted() => Status.Status.ShouldEqual(OrganizationSetupAcceptanceStatus.Accepted);
    [Fact] void should_use_the_organization_in_the_registration_fact() => Status.OrganizationName.Value.ShouldEqual("Registered Org");
}
#endif
