// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.UserSetup.for_OneUseJoinTenantInvitationConstraint.when_accepting_an_invitation;

public class and_it_has_already_been_accepted : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    readonly EventScenario _scenario = new();
    IAppendResult _result = null!;

    async Task Establish() =>
        await _scenario.Given.ForEventSource(_invitationId).Events(
            new InvitationToJoinTenantAccepted("Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Member"]));

    async Task Because() =>
        _result = await _scenario.EventLog.Append(
            _invitationId,
            new InvitationToJoinTenantAccepted("Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Member"]));

    [Fact] void should_be_failed() => _result.ShouldBeFailed();

    [Fact]
    void should_violate_the_one_use_invitation_constraint() =>
        _result.ShouldHaveConstraintViolationFor(UserSetupConstraintNames.OneUseInvitation);
}
#endif
