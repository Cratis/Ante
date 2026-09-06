// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.Reactors;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives;

public class and_a_join_tenant_invitation_arrives : Specification
{
    static readonly EventSourceId _invitationId = (EventSourceId)Guid.NewGuid().ToString();
    static readonly UserInvitedToJoinTenant _invited = new("jane@example.com", "Acme", ["Member"]);

    readonly ReactorScenario<IncomingInvitationReactor> _scenario = new();

    async Task Because() => await _scenario.Given.ForEventSource(_invitationId).Events(_invited);

    [Fact]
    void should_record_the_invitation_locally() =>
        _scenario.ShouldHaveProduced<JoinTenantInvitationReceived>(e => e.Email == _invited.Email && e.TenantName == _invited.TenantName);
}
#endif
