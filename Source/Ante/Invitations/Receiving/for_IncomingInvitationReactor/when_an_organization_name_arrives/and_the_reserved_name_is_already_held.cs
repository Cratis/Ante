// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;
using Ante.Invitations.OrganizationSetup;
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_organization_name_arrives.given;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_organization_name_arrives;

public class and_the_reserved_name_is_already_held : a_name_delivery
{
    void Establish() => AppendOutcome = new AppendResult
    {
        ConstraintViolations = [new ConstraintViolation(
            typeof(Ante.Organization.Names.OrganizationNameReservationReceived).GetEventType().Id,
            EventSequenceNumber.First,
            ConstraintType.Unique,
            OrganizationSetupConstraintNames.UniqueOrganizationName,
            "An organization with this name already exists.",
            [])],
    };

    Task Because() => Deliver(new OrganizationNameReserved("Acme"));

    [Fact] void should_acknowledge_the_delivery() => Assert.Null(Error);
}
#endif
