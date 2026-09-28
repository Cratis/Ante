// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_organization_name_arrives.given;
using Ante.Organization.Names;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_organization_name_arrives;

public class and_a_name_is_released : a_name_delivery
{
    void Establish() => Claims =
    [
        new OrganizationNameClaim("registration-1", "ACME"),
        new OrganizationNameClaim("organization-name-acme", "Acme"),
        new OrganizationNameClaim("registration-2", "Northwind"),
    ];

    Task Because() => Deliver(new OrganizationNameReleased("acme"));

    [Fact] void should_release_the_claim_from_the_onboarding() =>
        ShouldHaveAppended<OrganizationNameReleaseReceived>("registration-1", released => released.TenantName == "ACME");

    [Fact] void should_release_the_host_reservation() =>
        ShouldHaveAppended<OrganizationNameReleaseReceived>("organization-name-acme", released => released.TenantName == "Acme");

    [Fact] void should_leave_other_names_claimed() => Log.DidNotReceive().Append(
        (EventSourceId)"registration-2",
        Arg.Any<object>(),
        Arg.Any<EventStreamType>(),
        Arg.Any<EventStreamId>(),
        Arg.Any<EventSourceType>(),
        Arg.Any<Cratis.Execution.CorrelationId>(),
        Arg.Any<IEnumerable<string>>(),
        Arg.Any<Cratis.Chronicle.EventSequences.Concurrency.ConcurrencyScope>(),
        Arg.Any<DateTimeOffset?>(),
        Arg.Any<Cratis.Chronicle.Subject>());
}
#endif
