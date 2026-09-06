// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;

namespace Ante.Invitations.OrganizationSetup.for_UniqueOrganizationNameConstraint.when_appending_a_new_name;

public class and_no_organization_has_claimed_it_yet : Specification
{
    readonly EventScenario _scenario = new();
    IAppendResult _invitationAcceptedResult = null!;
    IAppendResult _registrationCompletedResult = null!;

    async Task Because()
    {
        _invitationAcceptedResult = await _scenario.EventLog.Append(
            InvitationId.New(),
            new InvitationToCreateTenantAccepted("Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Owner"]));

        _registrationCompletedResult = await _scenario.EventLog.Append(
            InvitationId.New(),
            new OrganizationRegistrationCompleted("Northwind", "sub-2", "github", "John", MiddleName.NotSet, "Smith", "john@example.com"));
    }

    [Fact] void should_succeed_claiming_the_name_from_an_accepted_invitation() => _invitationAcceptedResult.ShouldBeSuccessful();
    [Fact] void should_succeed_claiming_the_name_from_a_self_service_registration() => _registrationCompletedResult.ShouldBeSuccessful();
}
#endif
