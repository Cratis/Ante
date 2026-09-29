// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.for_query_access;
using Ante.Invitations.OrganizationSetup.for_OrganizationPublicationStatusNotifier.given;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationPublicationStatusNotifier.when_projection_rows_lag;

public class and_invited_organization_was_published : an_organization_notification
{
    void Establish()
    {
        Local.AddRange([Invited(), Legal()]);
        Outbox.AddRange([Invited(), Legal()]);
        Notifier = new(
            QueryCollections.With(new OrganizationSetupProgress(Id, "Stale Org", LegalRecorded: false)),
            QueryCollections.With(new OrganizationSetupPublished(Id, AcceptancePublished: false, LegalPublished: false)),
            Subscriptions,
            Store);
    }

    async Task Because() => await Notifier.NotifyIfPublished(Id.Value.ToString("D"));

    [Fact] void should_mark_status_accepted() => Status.Status.ShouldEqual(OrganizationSetupAcceptanceStatus.Accepted);
    [Fact] void should_use_the_organization_in_the_recorded_fact() => Status.OrganizationName.Value.ShouldEqual("Invited Org");
}
#endif
