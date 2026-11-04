// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.for_InvitationContentOptionsValidator.when_validating;

public class and_both_journeys_are_bound_from_configuration : Specification
{
    AnteOptions _options;
    Exception? _error;

    void Because()
    {
        var section = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ante:Invitations:UserSetup:Content:en:Title"] = "User copy",
            ["Ante:Invitations:UserSetup:Content:en:Intro"] = "<b>Literal markup</b>",
            ["Ante:Invitations:UserSetup:Content:en:SupportUrl"] = "https://example.com/support?journey=user",
            ["Ante:Invitations:OrganizationSetup:Content:nb:CompletionMessage"] = "Organization completion",
            ["Ante:Invitations:OrganizationSetup:Content:nb:SupportUrl"] = "",
            ["Ante:Invitations:Token:KeyId"] = "unchanged-token-configuration",
        }).Build().GetSection("Ante");
        _options = section.Get<AnteOptions>()!;
        _error = Cratis.Specifications.Catch.Exception(() => InvitationContentOptionsValidator.Validate(_options, section));
    }

    [Fact] void should_accept_literal_text_https_and_empty_links() => Assert.Null(_error);
    [Fact] void should_bind_user_copy() => _options.Invitations.UserSetup.Content["en"].Title.ShouldEqual("User copy");
    [Fact] void should_bind_organization_copy() => _options.Invitations.OrganizationSetup.Content["nb"].CompletionMessage.ShouldEqual("Organization completion");
    [Fact] void should_not_change_registration_defaults() => _options.Registration.Content.ShouldBeEmpty();
}
#endif
