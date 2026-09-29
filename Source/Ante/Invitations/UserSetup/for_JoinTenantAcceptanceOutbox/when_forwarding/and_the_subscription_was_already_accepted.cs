// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.UserSetup.for_JoinTenantAcceptanceOutbox.given;

namespace Ante.Invitations.UserSetup.for_JoinTenantAcceptanceOutbox.when_forwarding;

/// <summary>
/// A subscription another replica's durable read - or an earlier delivery - has already moved to accepted
/// must never be moved back to recorded when the acceptance event is delivered to the reactor again.
/// </summary>
public class and_the_subscription_was_already_accepted : a_forward_with_an_open_subscription
{
    List<UserSetupAcceptanceStatus> _observed = null!;

    void Establish()
    {
        _subscriptions.GetStatus(_id, isRecorded: true, isFullyPublished: true);
        _observed = Watch();
    }

    async Task Because() => await _scenario.Given.ForEventSource(EventSource).Events(_accepted);

    [Fact] void should_only_ever_have_been_accepted() => Assert.All(_observed, status => Assert.Equal(UserSetupAcceptanceStatus.Accepted, status));
}
#endif
