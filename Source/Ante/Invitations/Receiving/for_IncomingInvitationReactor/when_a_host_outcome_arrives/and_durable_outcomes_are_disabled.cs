// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Onboarding;
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_organization_name_arrives.given;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_a_host_outcome_arrives;

public class and_durable_outcomes_are_disabled : a_name_delivery
{
    Task Because() => Deliver(new HostProvisioningAttemptStarted(Guid.NewGuid(), "unused-proof"));

    [Fact] void should_ignore_the_stray_outcome() => Assert.Null(Error);
    [Fact] void should_not_append_anything() => Assert.Empty(Log.ReceivedCalls());
    [Fact] void should_not_read_projections() => Assert.Empty(ReadModels.ReceivedCalls());
}
#endif
