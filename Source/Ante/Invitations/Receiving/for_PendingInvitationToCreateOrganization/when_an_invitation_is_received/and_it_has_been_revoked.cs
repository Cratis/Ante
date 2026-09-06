// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Receiving.for_PendingInvitationToCreateOrganization.when_an_invitation_is_received;

public class and_it_has_been_revoked : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    ReadModelScenario<PendingInvitationToCreateOrganization> _scenario = null!;

    void Establish() => _scenario = new();

    async Task Because() =>
        await _scenario.Given.ForEventSource(_invitationId).Events(
            new CreateTenantInvitationReceived("jane@example.com", ["Owner"]),
            new InvitationRevocationReceived());

    [Fact] void should_no_longer_be_pending() => Assert.Null(_scenario.Instance);
}
#endif
