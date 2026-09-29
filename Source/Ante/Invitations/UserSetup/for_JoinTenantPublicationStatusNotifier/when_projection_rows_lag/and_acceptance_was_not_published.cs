// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.UserSetup.for_JoinTenantPublicationStatusNotifier.given;

namespace Ante.Invitations.UserSetup.for_JoinTenantPublicationStatusNotifier.when_projection_rows_lag;

public class and_acceptance_was_not_published : a_join_notification
{
    void Establish() => Local.Add(Accepted());

    async Task Because() => await Notifier.NotifyIfPublished(Id.Value.ToString("D"));

    [Fact] void should_keep_status_pending() => Status.ShouldEqual(UserSetupAcceptanceStatus.Pending);
}
#endif
