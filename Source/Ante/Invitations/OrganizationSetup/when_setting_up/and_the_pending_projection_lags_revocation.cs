// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Invitations.Accepting;
using Ante.Invitations.Receiving;
using Ante.Legal;
using Ante.Organization.Names;
using Cratis.Chronicle.EventSequences.Concurrency;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.OrganizationSetup.when_setting_up;

public class and_the_pending_projection_lags_revocation : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    readonly CommandScenario<SetupOrganization> _scenario = new();
    readonly IInvitationAcceptanceFence _fence = Substitute.For<IInvitationAcceptanceFence>();
    CommandResult _result = null!;

    void Establish()
    {
        _scenario.Given.ForEventSource(_invitationId).ReadModel(
            new PendingInvitationToCreateOrganization(_invitationId, Guid.NewGuid(), "jane@example.com", ["Owner"]));

        var signedInIdentity = Substitute.For<ISignedInIdentity>();
        signedInIdentity.Resolve(_invitationId, Arg.Any<Cratis.Chronicle.Subject>()).Returns(((IdentityProviderName)"github", (Cratis.Chronicle.Subject)"subject-1"));
        signedInIdentity.IsVerifiedOwnerOf(_invitationId).Returns(true);
        _fence.For(_invitationId, InvitationFlowType.CreateTenant).Returns((ConcurrencyScope?)null);

        var acceptedNames = Substitute.For<IClaimedOrganizationNameSearch>();
        acceptedNames.Contains(Arg.Any<string>()).Returns(false);
        _scenario.Services.AddSingleton(acceptedNames);
        _scenario.Services.AddSingleton(signedInIdentity);
        _scenario.Services.AddSingleton(_fence);
        _scenario.Services.AddSingleton<ILegalDocumentSource>(new NoLegalDocumentSource());
    }

    async Task Because() =>
        _result = await _scenario.Execute(new SetupOrganization(_invitationId, "Acme", "Jane", null, "Doe", false, LegalVersion.NotSet));

    [Fact] void should_reject_the_stale_invitation() => _result.ShouldHaveValidationErrors();
    [Fact] void should_not_append_an_acceptance() => Assert.Empty(_scenario.AppendedEvents);
    [Fact] async Task should_consult_the_authoritative_fence() => await _fence.Received(1).For(_invitationId, InvitationFlowType.CreateTenant);
}
#endif
