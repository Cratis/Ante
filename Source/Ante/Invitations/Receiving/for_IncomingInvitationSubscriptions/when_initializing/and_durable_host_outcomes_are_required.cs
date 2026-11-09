// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Onboarding;
using Ante.Onboarding.Provisioning;

namespace Ante.Invitations.Receiving.for_IncomingInvitationSubscriptions.when_initializing;

public class and_durable_host_outcomes_are_required : given.a_lobby_with
{
    static readonly EventTypeId[] _outcomes =
    [
        typeof(HostProvisioningAttemptStarted).GetEventType().Id,
        typeof(HostProvisioningSucceeded).GetEventType().Id,
        typeof(HostProvisioningFailed).GetEventType().Id,
        typeof(HostProvisioningRequiresNewInvitation).GetEventType().Id,
    ];

    void Establish() => _hostOutcomes = new() { Mode = HostOutcomesMode.DurableRequired };

    Task Because() => Initialize();

    [Fact] void should_subscribe_to_every_host_outcome() => Assert.All(_outcomes, outcome => Assert.Contains(outcome, _subscriptions[0].EventTypes));
}
#endif
