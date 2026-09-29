// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.UserSetup.for_JoinTenantPublicationStatusNotifier.given;

namespace Ante.Invitations.UserSetup.for_JoinTenantPublicationStatusNotifier.when_recording;

public class and_the_join_acceptance_is_recorded : a_join_notification
{
    readonly List<UserSetupAcceptanceStatus> _observed = [];

    void Establish() => Subscriptions.GetStatus(Id).Subscribe(view => _observed.Add(view.Status));

    async Task Because() => await Notifier.NotifyRecorded((EventSourceId)Id.Value.ToString("D"), Accepted().Content);

    [Fact] void should_push_recorded_to_the_open_subscription() => Assert.Equal([UserSetupAcceptanceStatus.Pending, UserSetupAcceptanceStatus.Recorded], _observed);
}
#endif
