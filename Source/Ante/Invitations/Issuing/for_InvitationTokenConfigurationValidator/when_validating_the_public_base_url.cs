// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Issuing.for_InvitationTokenConfigurationValidator;

public class when_validating_the_public_base_url : Specification
{
    [Fact]
    void should_refuse_non_absolute_non_https_and_ambiguous_base_urls()
    {
        foreach (var value in new[] { "/relative", "//lobby.example.com", "http://lobby.example.com", "ftp://localhost", "https://user:password@lobby.example.com", "https://lobby.example.com?query=value", "https://lobby.example.com#fragment" })
        {
            var error = Assert.Throws<InvitationTokenConfigurationInvalid>(() => InvitationTokenConfigurationValidator.Validate(new InvitationTokenConfig { PublicBaseUrl = value }));
            Assert.Contains("PublicBaseUrl", error.Message);
            Assert.DoesNotContain(value, error.Message);
        }
    }

    [Fact]
    void should_allow_https_and_loopback_http_for_development()
    {
        foreach (var value in new[] { "https://lobby.example.com", "https://lobby.example.com/onboarding/", "http://localhost:5050", "http://127.0.0.1:5050", "http://[::1]:5050" })
        {
            InvitationTokenConfigurationValidator.Validate(new InvitationTokenConfig { PublicBaseUrl = value });
        }
    }

    [Fact]
    void should_allow_disabling_metadata_without_a_base_url()
    {
        foreach (var value in new[] { string.Empty, " " })
        {
            InvitationTokenConfigurationValidator.Validate(new InvitationTokenConfig { PublicBaseUrl = value });
        }
    }
}
#endif
