// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Invitations.Accepting;
using Ante.Legal;
using MongoDB.Driver;

namespace Ante.Invitations.OrganizationSetup.when_setting_up;

public class and_middle_name_contains_a_text_direction_override_character : Specification
{
    // Code point 0x202E is the right-to-left override - a Unicode "format" character that can make text
    // render in a different order than it is stored, which has no legitimate place in a personal name.
    // Composed from its numeric code point rather than written as a literal byte in the source, so the
    // file itself never carries an actual, invisible override character.
    static readonly string _middleNameWithDirectionOverride = ((char)0x202E) + "evil";

    FluentValidation.Results.ValidationResult _result = null!;

    async Task Because()
    {
        var invitationId = InvitationId.New();

        var acceptedNames = Substitute.For<IMongoCollection<AcceptedOrganizationName>>();
        acceptedNames.CountDocumentsAsync(Arg.Any<FilterDefinition<AcceptedOrganizationName>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(0L);

        var signedInIdentity = Substitute.For<ISignedInIdentity>();
        signedInIdentity.IsVerifiedOwnerOf(invitationId).Returns(true);

        var validator = new SetupOrganizationValidator(new NoLegalDocumentSource(), acceptedNames, signedInIdentity);
        _result = await validator.ValidateAsync(new SetupOrganization(invitationId, "Acme", "Jane", _middleNameWithDirectionOverride, "Doe", false, LegalVersion.NotSet));
    }

    [Fact] void should_not_be_valid() => Assert.False(_result.IsValid);

    [Fact]
    void should_report_the_middle_name_field() =>
        Assert.Contains(_result.Errors, e => e.PropertyName == nameof(SetupOrganization.MiddleName));
}
#endif
