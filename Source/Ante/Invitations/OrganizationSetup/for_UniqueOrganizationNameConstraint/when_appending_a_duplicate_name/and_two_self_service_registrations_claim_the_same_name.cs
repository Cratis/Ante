// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;

namespace Ante.Invitations.OrganizationSetup.for_UniqueOrganizationNameConstraint.when_appending_a_duplicate_name;

public class and_two_self_service_registrations_claim_the_same_name : Specification
{
    readonly EventScenario _scenario = new();
    IAppendResult _result = null!;

    async Task Establish() =>
        await _scenario.Given.ForEventSource(InvitationId.New()).Events(
            new OrganizationRegistrationCompleted("Acme", "sub-1", "github", "Jane", MiddleName.NotSet, "Doe", "jane@example.com"));

    async Task Because() =>
        _result = await _scenario.EventLog.Append(
            InvitationId.New(),
            new OrganizationRegistrationCompleted("Acme", "sub-2", "github", "John", MiddleName.NotSet, "Smith", "john@example.com"));

    [Fact] void should_be_failed() => _result.ShouldBeFailed();

    [Fact]
    void should_violate_the_unique_organization_name_constraint() =>
        _result.ShouldHaveConstraintViolationFor(OrganizationSetupConstraintNames.UniqueOrganizationName);
}
#endif
