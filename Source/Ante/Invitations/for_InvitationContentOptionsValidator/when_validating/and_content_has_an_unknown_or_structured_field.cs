// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.for_InvitationContentOptionsValidator.when_validating;

public class and_content_has_an_unknown_or_structured_field : Specification
{
    readonly List<Exception?> _errors = [];

    void Because()
    {
        foreach (var field in new[] { "LegalText", "ValidationMessage", "Resources:common.accept", "Title:Html" })
        {
            var section = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"Ante:Invitations:OrganizationSetup:Content:en:{field}"] = "Not allowed",
            }).Build().GetSection("Ante");
            _errors.Add(Cratis.Specifications.Catch.Exception(() => InvitationContentOptionsValidator.Validate(section.Get<AnteOptions>()!, section)));
        }
    }

    [Fact] void should_reject_non_allowlisted_content() => Assert.All(_errors, error => Assert.IsType<InvitationContentConfigurationInvalid>(error));
}
#endif
