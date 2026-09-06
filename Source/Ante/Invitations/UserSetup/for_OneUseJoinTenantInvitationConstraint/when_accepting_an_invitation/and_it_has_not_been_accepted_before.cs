// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.UserSetup.for_OneUseJoinTenantInvitationConstraint.when_accepting_an_invitation;

public class and_it_has_not_been_accepted_before : Specification
{
    readonly EventScenario _scenario = new();
    IAppendResult _result = null!;

    async Task Because() =>
        _result = await _scenario.EventLog.Append(
            InvitationId.New(),
            new InvitationToJoinTenantAccepted("Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Member"]));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
}
#endif
