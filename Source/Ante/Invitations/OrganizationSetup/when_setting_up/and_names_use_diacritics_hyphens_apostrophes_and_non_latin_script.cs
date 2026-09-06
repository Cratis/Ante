// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Invitations.Accepting;
using Ante.Legal;
using MongoDB.Driver;

namespace Ante.Invitations.OrganizationSetup.when_setting_up;

public class and_names_use_diacritics_hyphens_apostrophes_and_non_latin_script : Specification
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

        // "Zoë-Renée" (diacritics + hyphen), "田中" (non-Latin script) and "O'Brien" (apostrophe) are all
        // real, legitimate name shapes that must not be treated as invalid input.
        _result = await validator.ValidateAsync(new SetupOrganization(invitationId, "Acme", "Zoë-Renée", "田中", "O'Brien", false, LegalVersion.NotSet));
    }

    [Fact] void should_be_valid() => Assert.True(_result.IsValid);
}
#endif
