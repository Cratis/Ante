// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_organization_name_arrives.given;
using Ante.Organization.Names;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_organization_name_arrives;

public class and_a_name_is_released_while_a_claim_has_no_name : a_name_delivery
{
    void Establish() => Claims =
    [
        new OrganizationNameClaim("registration-1", null!),
        new OrganizationNameClaim("organization-name-acme", "ACME"),
    ];

    Task Because() => Deliver(new OrganizationNameReleased("acme"));

    [Fact] void should_not_fail() => Assert.Null(Error);

    [Fact] void should_release_the_claim_that_carries_the_name() =>
        ShouldHaveAppended<OrganizationNameReleaseReceived>("organization-name-acme", released => released.TenantName == "ACME");
}
#endif
