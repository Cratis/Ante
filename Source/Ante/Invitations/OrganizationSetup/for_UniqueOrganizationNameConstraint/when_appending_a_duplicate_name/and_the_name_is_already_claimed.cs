// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.OrganizationSetup.for_UniqueOrganizationNameConstraint.when_appending_a_duplicate_name;

public class and_the_name_is_already_claimed : Specification
{
    readonly EventScenario _scenario = new();
    IAppendResult _result = null!;

    async Task Establish() =>
        await _scenario.Given.ForEventSource(InvitationId.New()).Events(
            new InvitationToCreateTenantAccepted("Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Owner"]));

    async Task Because() =>
        _result = await _scenario.EventLog.Append(
            InvitationId.New(),
            new InvitationToCreateTenantAccepted("Acme", "github", "sub-2", "John", MiddleName.NotSet, "Smith", "john@example.com", ["Owner"]));

    [Fact] void should_be_failed() => _result.ShouldBeFailed();

    [Fact]
    void should_violate_the_unique_organization_name_constraint() =>
        _result.ShouldHaveConstraintViolationFor(OrganizationSetupConstraintNames.UniqueOrganizationName);
}
#endif
