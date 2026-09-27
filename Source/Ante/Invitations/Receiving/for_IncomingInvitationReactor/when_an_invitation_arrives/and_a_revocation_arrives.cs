// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.Reactors;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives;

public class and_a_revocation_arrives : Specification
{
    static readonly EventSourceId _invitationId = (EventSourceId)Guid.NewGuid().ToString();

    readonly ReactorScenario<IncomingInvitationScenarioReactor<object>> _scenario = new(new ServiceCollection()
        .AddSingleton(Substitute.For<IEventStore>())
        .AddLogging()
        .BuildServiceProvider());

    async Task Because() => await _scenario.Given.ForEventSource(_invitationId).Events(new InvitationRevoked());

    [Fact]
    void should_record_the_revocation_locally() => _scenario.ShouldHaveProduced<InvitationRevocationReceived>();
}
#endif
