// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Receiving.for_PendingInvitationToJoin.when_an_invitation_is_received;

public class and_it_has_not_been_accepted_or_revoked : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    ReadModelScenario<PendingInvitationToJoin> _scenario = null!;

    void Establish() => _scenario = new();

    async Task Because() =>
        await _scenario.Given.ForEventSource(_invitationId).Events(
            new JoinTenantInvitationReceived("jane@example.com", "Acme", ["Member"]));

    [Fact] void should_have_an_instance() => Assert.NotNull(_scenario.Instance);
    [Fact] void should_carry_the_email() => Assert.Equal((Email)"jane@example.com", _scenario.Instance!.Email);
    [Fact] void should_carry_the_tenant_name() => Assert.Equal((TenantName)"Acme", _scenario.Instance!.TenantName);
}
#endif
