// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Contracts.Organization;
using Ante.Invitations.Accepting;
using Ante.Invitations.OrganizationSetup;
using Ante.Invitations.Receiving;
using Ante.Legal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ante.Invitations.UserSetup.when_accepting;

public class and_a_registration_already_used_the_invitation_id : Specification
{
    readonly InvitationId _id = InvitationId.New();
    readonly CommandScenario<AcceptInvitation> _scenario = new();
    CommandResult _result = null!;

    void Establish()
    {
        _scenario.Given.ForEventSource(_id).Events(new OrganizationRegistrationCompleted(
            "Acme", "sub-1", "github", "Jane", MiddleName.NotSet, "Doe", "jane@example.com"));
        _scenario.Given.ForEventSource(_id).ReadModel(new PendingInvitationToJoin(_id, Guid.NewGuid(), "jane@example.com", "Acme", ["Member"]));
        var identity = Substitute.For<ISignedInIdentity>();
        identity.IsVerifiedOwnerOf(_id).Returns(true);
        identity.Resolve(_id, Arg.Any<Cratis.Chronicle.Subject>()).Returns(((IdentityProviderName)"github", new Cratis.Chronicle.Subject("sub-1")));
        var backchannel = Substitute.For<IIdentityBackchannel>();
        backchannel.IsSubjectAlreadyAssociatedWithAUser(Arg.Any<TenantName>(), Arg.Any<string>()).Returns(false);
        _scenario.Services.AddSingleton(identity);
        _scenario.Services.AddSingleton(backchannel);
        _scenario.Services.AddSingleton<ILegalDocumentSource>(new NoLegalDocumentSource());
        _scenario.Services.AddSingleton(Substitute.For<IHttpContextAccessor>());
        _scenario.Services.AddSingleton(new UserSetupStatusSubscriptions());
    }

    async Task Because() =>
        _result = await _scenario.Execute(new AcceptInvitation(_id, "Jane", null, "Doe", false, LegalVersion.NotSet));

    [Fact] void should_reject_the_invitation() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_no_exceptions() => _result.ShouldNotHaveExceptions();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
}
#endif
