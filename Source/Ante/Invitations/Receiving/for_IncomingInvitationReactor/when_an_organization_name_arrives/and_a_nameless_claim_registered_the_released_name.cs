// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_organization_name_arrives.given;
using Ante.Organization.Names;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_organization_name_arrives;

/// <summary>
/// A claim projected before its name was mapped explicitly carries no name until the projection is replayed;
/// its own events still say which name it holds, so the release must not be dropped.
/// </summary>
public class and_a_nameless_claim_registered_the_released_name : a_name_delivery
{
    void Establish()
    {
        Claims = [new OrganizationNameClaim("registration-1", null!)];
        Histories["registration-1"] =
        [
            new OrganizationRegistrationCompleted("ACME", "sub-1", "github", "Jane", MiddleName.NotSet, "Doe", "jane@example.com"),
        ];
    }

    Task Because() => Deliver(new OrganizationNameReleased("acme"));

    [Fact] void should_not_fail() => Assert.Null(Error);

    [Fact] void should_release_the_claim_with_the_name_from_its_events() =>
        ShouldHaveAppended<OrganizationNameReleaseReceived>("registration-1", released => released.TenantName == "ACME");
}
#endif
