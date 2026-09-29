// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.for_query_access;
using Ante.Invitations.UserSetup.for_JoinTenantPublicationStatusNotifier.given;

namespace Ante.Invitations.UserSetup.for_JoinTenantPublicationStatusNotifier.when_projection_rows_lag;

public class and_all_recorded_facts_were_published : a_join_notification
{
    void Establish()
    {
        Local.AddRange([Accepted(), Legal()]);
        var published = Accepted();
        Outbox.Add(published with { Context = published.Context with { EventType = new(typeof(InvitationToJoinTenantAccepted).GetEventType().Id, 2) } });
        Outbox.Add(Legal());
        Notifier = new(
            QueryCollections.With(new UserSetupProgress(Id, AcceptanceRecorded: false, LegalRecorded: true)),
            QueryCollections.With(new JoinTenantAcceptancePublished(Id, AcceptancePublished: false, LegalPublished: false)),
            Subscriptions,
            Store);
    }

    async Task Because() => await Notifier.NotifyIfPublished(Id.Value.ToString("D"));

    [Fact] void should_mark_status_accepted_even_if_the_published_generation_differs() => Status.ShouldEqual(UserSetupAcceptanceStatus.Accepted);
}
#endif
