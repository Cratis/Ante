// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.for_InvitationContentOptionsValidator.when_validating;

public class and_a_support_link_is_invalid : Specification
{
    readonly List<Exception?> _errors = [];

    void Because()
    {
        foreach (var journey in new[] { "UserSetup", "OrganizationSetup" })
        {
            foreach (var url in new[] { "http://example.com", "javascript:alert(1)", "/support", "https://", "not a URL", "https://example.com/with spaces" })
            {
                var section = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"Ante:Invitations:{journey}:Content:en:SupportUrl"] = url,
                }).Build().GetSection("Ante");
                _errors.Add(Cratis.Specifications.Catch.Exception(() => InvitationContentOptionsValidator.Validate(section.Get<AnteOptions>()!, section)));
            }
        }
    }

    [Fact] void should_fail_startup_for_both_journeys() => Assert.All(_errors, error => Assert.IsType<InvitationContentConfigurationInvalid>(error));
    [Fact] void should_identify_the_invalid_setting() => Assert.All(_errors, error => Assert.Contains(":SupportUrl", error!.Message));
}
#endif
