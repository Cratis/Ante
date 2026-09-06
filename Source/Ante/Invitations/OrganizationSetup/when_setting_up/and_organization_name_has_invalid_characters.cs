// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Invitations.Accepting;
using Ante.Legal;
using MongoDB.Driver;

namespace Ante.Invitations.OrganizationSetup.when_setting_up;

public class and_organization_name_has_invalid_characters : Specification
{
    FluentValidation.Results.ValidationResult _result = null!;

    async Task Because()
    {
        var invitationId = InvitationId.New();

        var acceptedNames = Substitute.For<IMongoCollection<AcceptedOrganizationName>>();
        acceptedNames.CountDocumentsAsync(Arg.Any<FilterDefinition<AcceptedOrganizationName>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(0L);

        var signedInIdentity = Substitute.For<ISignedInIdentity>();
        signedInIdentity.IsVerifiedOwnerOf(invitationId).Returns(true);

        var validator = new SetupOrganizationValidator(new NoLegalDocumentSource(), acceptedNames, signedInIdentity);
        _result = await validator.ValidateAsync(new SetupOrganization(invitationId, "Acme/Corp", "Jane", null, "Doe", false, LegalVersion.NotSet));
    }

    [Fact] void should_not_be_valid() => Assert.False(_result.IsValid);

    [Fact]
    void should_report_the_organization_name_field() =>
        Assert.Contains(_result.Errors, e => e.PropertyName == nameof(SetupOrganization.OrganizationName));
}
#endif
