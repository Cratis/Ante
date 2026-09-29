// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.OrganizationSetup.for_OrganizationSetupOutbox.given;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupOutbox.when_forwarding;

/// <summary>
/// A subscription another replica's durable read - or an earlier delivery - has already moved to accepted
/// must never be moved back to recorded when the acceptance event is delivered to the reactor again.
/// </summary>
public class and_the_subscription_was_already_accepted : a_forward_with_an_open_subscription
{
    List<OrganizationSetupAcceptanceStatus> _observed = null!;

    void Establish()
    {
        _subscriptions.GetStatus(_id, "Acme", isRecorded: true, isFullyPublished: true);
        _observed = Watch();
    }

    async Task Because() => await _scenario.Given.ForEventSource(EventSource).Events(_accepted);

    [Fact] void should_only_ever_have_been_accepted() => Assert.All(_observed, status => Assert.Equal(OrganizationSetupAcceptanceStatus.Accepted, status));
}
#endif
