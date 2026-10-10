// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Invitations.Accepting;
using Ante.Invitations.Accepting.for_InvitationAcceptanceFence;
using Ante.Invitations.Receiving;
using Ante.Legal;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.UserSetup.when_accepting;

// The organization refuses a login it already knows. That has to leave the invitation exactly as it was -
// nothing recorded as accepted, nothing claimed - so the invitee can still use it with another login
// (Cratis/StudioIssues#581).
public class and_the_login_is_already_associated_with_a_user : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    readonly CommandScenario<AcceptInvitation> _scenario = new();
    CommandResult _result = null!;

    void Establish()
    {
        var pending = new PendingInvitationToJoin(_invitationId, Guid.NewGuid(), "jane@example.com", "Acme", ["Member"]);
        _scenario.Given.ForEventSource(_invitationId).ReadModel(pending);

        var signedInIdentity = Substitute.For<ISignedInIdentity>();
        signedInIdentity.Resolve(_invitationId, Arg.Any<Cratis.Chronicle.Subject>()).Returns(((IdentityProviderName)"github", new Cratis.Chronicle.Subject("already-known")));
        signedInIdentity.IsVerifiedOwnerOf(_invitationId).Returns(true);

        var backchannel = Substitute.For<IIdentityBackchannel>();
        backchannel.IsSubjectAlreadyAssociatedWithAUser(Arg.Any<TenantName>(), Arg.Any<string>()).Returns(true);

        _scenario.Services.AddSingleton(signedInIdentity);
        _scenario.Services.AddSingleton(backchannel);
        _scenario.Services.AddSingleton(AcceptanceFenceForSpecs.Allow(_invitationId, InvitationFlowType.JoinTenant));
        _scenario.Services.AddSingleton<ILegalDocumentSource>(new NoLegalDocumentSource());
        _scenario.Services.AddSingleton(new UserSetupStatusSubscriptions());
    }

    async Task Because() =>
        _result = await _scenario.Execute(new AcceptInvitation(_invitationId, "Jane", null, "Doe", false, LegalVersion.NotSet));

    [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
    [Fact] void should_report_why() => _result.ShouldHaveValidationErrors();
    [Fact] void should_not_consume_the_invitation() => Assert.DoesNotContain(_scenario.AppendedEvents, e => e.Event.Content is InvitationToJoinTenantAccepted);
    [Fact] void should_not_claim_the_onboarding_attempt() => Assert.DoesNotContain(_scenario.AppendedEvents, e => e.Event.Content is OnboardingAttemptClaimed);
}
#endif
