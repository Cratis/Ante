// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Receiving.for_PendingInvitationToJoin.when_an_invitation_is_received;

public class and_it_has_since_been_accepted : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    ReadModelScenario<PendingInvitationToJoin> _scenario = null!;

    void Establish() => _scenario = new();

    async Task Because() =>
        await _scenario.Given.ForEventSource(_invitationId).Events(
            new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]),
            new InvitationToJoinTenantAccepted("Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Member"]));

    [Fact] void should_no_longer_be_pending() => Assert.Null(_scenario.Instance);
}
#endif
