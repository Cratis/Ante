// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Invitations.Accepting;
using Ante.Invitations.Receiving;
using Ante.Legal;
using Cratis.Chronicle.EventSequences.Concurrency;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.UserSetup.when_accepting;

public class and_the_pending_projection_lags_revocation : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    readonly CommandScenario<AcceptInvitation> _scenario = new();
    readonly IInvitationAcceptanceFence _fence = Substitute.For<IInvitationAcceptanceFence>();
    CommandResult _result = null!;

    void Establish()
    {
        _scenario.Given.ForEventSource(_invitationId).ReadModel(
            new PendingInvitationToJoin(_invitationId, Guid.NewGuid(), "jane@example.com", "Acme", ["Member"]));

        var signedInIdentity = Substitute.For<ISignedInIdentity>();
        signedInIdentity.Resolve(_invitationId, Arg.Any<Cratis.Chronicle.Subject>()).Returns(((IdentityProviderName)"github", (Cratis.Chronicle.Subject)"subject-1"));
        signedInIdentity.IsVerifiedOwnerOf(_invitationId).Returns(true);
        _fence.For(_invitationId, InvitationFlowType.JoinTenant).Returns((ConcurrencyScope?)null);

        _scenario.Services.AddSingleton(signedInIdentity);
        _scenario.Services.AddSingleton(Substitute.For<IIdentityBackchannel>());
        _scenario.Services.AddSingleton(_fence);
        _scenario.Services.AddSingleton<ILegalDocumentSource>(new NoLegalDocumentSource());
    }

    async Task Because() =>
        _result = await _scenario.Execute(new AcceptInvitation(_invitationId, "Jane", null, "Doe", false, LegalVersion.NotSet));

    [Fact] void should_reject_the_stale_invitation() => _result.ShouldHaveValidationErrors();
    [Fact] void should_not_append_an_acceptance() => Assert.Empty(_scenario.AppendedEvents);
    [Fact] async Task should_consult_the_authoritative_fence() => await _fence.Received(1).For(_invitationId, InvitationFlowType.JoinTenant);
}
#endif
