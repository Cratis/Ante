// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Onboarding;
using Ante.Onboarding.Provisioning;

namespace Ante.Invitations.Receiving.for_IncomingInvitationSubscriptions.when_initializing;

public class and_durable_host_outcomes_are_disabled : given.a_lobby_with
{
    void Establish() => _hostOutcomes = new() { Mode = HostOutcomesMode.Disabled };

    Task Because() => Initialize();

    [Fact] void should_keep_the_released_subscription() => Assert.Equal(7, _subscriptions[0].EventTypes.Count());
    [Fact] void should_not_subscribe_to_host_outcomes() => Assert.DoesNotContain(typeof(HostProvisioningSucceeded).GetEventType().Id, _subscriptions[0].EventTypes);
}
#endif
