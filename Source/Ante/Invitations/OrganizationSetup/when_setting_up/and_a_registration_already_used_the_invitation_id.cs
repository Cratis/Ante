// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Invitations.Accepting;
using Ante.Invitations.Receiving;
using Ante.Legal;
using Ante.Organization.Names;
using Ante.Organization.Registration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Invitations.OrganizationSetup.when_setting_up;

public class and_a_registration_already_used_the_invitation_id : Specification
{
    readonly InvitationId _id = InvitationId.New();
    readonly CommandScenario<SetupOrganization> _scenario = new();
    CommandResult _result = null!;

    void Establish()
    {
        // Public completion notifications belong to the outbox; seed the durable registration facts.
        _scenario.Given.ForEventSource(_id).Events(
            new OnboardingAttemptClaimed(),
            new OrganizationRegistrationRecorded("Acme", "sub-1", "github", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", []));
        _scenario.Given.ForEventSource(_id).ReadModel(new PendingInvitationToCreateOrganization(_id, Guid.NewGuid(), "jane@example.com", ["Owner"]));
        var names = Substitute.For<IClaimedOrganizationNameSearch>();
        names.Contains(Arg.Any<string>()).Returns(false);
        var identity = Substitute.For<ISignedInIdentity>();
        identity.IsVerifiedOwnerOf(_id).Returns(true);
        _scenario.Services.AddSingleton(names);
        _scenario.Services.AddSingleton(identity);
        _scenario.Services.AddSingleton<ILegalDocumentSource>(new NoLegalDocumentSource());
        _scenario.Services.AddSingleton(Substitute.For<IHttpContextAccessor>());
        _scenario.Services.AddSingleton(new OrganizationSetupStatusSubscriptions());
    }

    async Task Because() =>
        _result = await _scenario.Execute(new SetupOrganization(_id, "Northwind", "Jane", null, "Doe", false, LegalVersion.NotSet));

    [Fact] void should_reject_the_invitation() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
}
#endif
