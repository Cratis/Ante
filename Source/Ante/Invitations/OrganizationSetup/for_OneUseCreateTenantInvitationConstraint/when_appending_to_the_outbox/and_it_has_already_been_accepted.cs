// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.given;

namespace Ante.Invitations.OrganizationSetup.for_OneUseCreateTenantInvitationConstraint.when_appending_to_the_outbox;

/// <summary>
/// The one-use rule is enforced where the acceptance is recorded, the event log. The outbox only holds the forwarded copy.
/// </summary>
public class and_it_has_already_been_accepted : an_outbox_scenario_with<OneUseCreateTenantInvitationConstraint>
{
    static readonly InvitationId _invitationId = InvitationId.New();
    IAppendResult _result = null!;

    async Task Establish() =>
        await Scenario.Given.ForEventSource(_invitationId).Events(
            new InvitationToCreateTenantAccepted("Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Owner"]));

    async Task Because() =>
        _result = await Scenario.EventLog.Append(
            _invitationId,
            new InvitationToCreateTenantAccepted("Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Owner"]));

    [Fact] void should_be_successful() => _result.ShouldBeSuccessful();
}
#endif
