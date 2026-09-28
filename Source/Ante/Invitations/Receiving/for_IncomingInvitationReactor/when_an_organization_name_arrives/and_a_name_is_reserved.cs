// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_organization_name_arrives.given;
using Ante.Organization.Names;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_organization_name_arrives;

public class and_a_name_is_reserved : a_name_delivery
{
    Task Because() => Deliver(new OrganizationNameReserved("Acme"));

    [Fact] void should_record_the_reservation_under_a_case_insensitive_source() =>
        ShouldHaveAppended<OrganizationNameReservationReceived>("organization-name-acme", received => received.TenantName == "Acme");

    [Fact] void should_succeed() => Assert.Null(Error);
}
#endif
