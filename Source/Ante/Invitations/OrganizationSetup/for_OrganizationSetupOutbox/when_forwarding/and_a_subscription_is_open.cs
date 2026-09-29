// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.OrganizationSetup.for_OrganizationSetupOutbox.given;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupOutbox.when_forwarding;

/// <summary>
/// An open status subscription must see the acceptance as recorded the moment the local acceptance event
/// reaches the forwarding reactor - before the outbox append - and then as accepted once it is published,
/// rather than jumping from pending straight to accepted.
/// </summary>
public class and_a_subscription_is_open : a_forward_with_an_open_subscription
{
    List<OrganizationSetupAcceptanceStatus> _observed = null!;

    void Establish() => _observed = Watch();

    async Task Because() => await _scenario.Given.ForEventSource(EventSource).Events(_accepted);

    [Fact]
    void should_see_pending_then_recorded_then_accepted() =>
        Assert.Equal(
            [OrganizationSetupAcceptanceStatus.Pending, OrganizationSetupAcceptanceStatus.Recorded, OrganizationSetupAcceptanceStatus.Accepted],
            _observed);

    [Fact] void should_forward_to_the_outbox_once() => VerifyAppendedOnce();
}
#endif
