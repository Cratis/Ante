// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.given;

namespace Ante.Invitations.OrganizationSetup.for_UniqueOrganizationNameConstraint.when_appending_to_the_outbox;

/// <summary>
/// A released name is freed in the event log but never in the outbox, so a second registration of that name must
/// still be forwarded to the outbox after the first one was published there (Cratis/Ante#138).
/// </summary>
public class and_the_name_is_already_claimed : an_outbox_scenario_with<UniqueOrganizationNameConstraint>
{
    IAppendResult _result = null!;

    async Task Establish() =>
        await Scenario.Given.ForEventSource(InvitationId.New()).Events(
            new InvitationToCreateTenantAccepted("Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Owner"]));

    async Task Because() =>
        _result = await Scenario.EventLog.Append(
            InvitationId.New(),
            new InvitationToCreateTenantAccepted("Acme", "github", "sub-2", "John", MiddleName.NotSet, "Smith", "john@example.com", ["Owner"]));

    [Fact] void should_be_successful() => _result.ShouldBeSuccessful();
}
#endif
