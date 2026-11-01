// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Ante.Onboarding.for_OnboardingConfiguration;

public class when_applying_the_contract : Specification
{
    string _directory = null!;
    AnteOptions _options = null!;
    ConfigurationManager _configuration = null!;

    void Establish()
    {
        _directory = Directory.CreateTempSubdirectory("ante-onboarding").FullName;
        File.WriteAllText(Path.Combine(_directory, "signing.pem"), "signing-key-placeholder");
        var secrets = Directory.CreateDirectory(Path.Combine(_directory, "secrets")).FullName;
        File.WriteAllText(Path.Combine(secrets, "Ante__Invitations__Token__PublicKeyPem"), "verification-placeholder\n");
        _configuration = new ConfigurationManager();
        _configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ante:HostAppUrl"] = "https://{tenant}.example.com/",
            ["Ante:Onboarding:Enabled"] = "true",
            ["Ante:Onboarding:HostStore"] = "Shop",
            ["Ante:Onboarding:PublicBaseUrl"] = "https://onboard.shop.example.com",
            ["Ante:Onboarding:ApplicationUrl"] = "https://shop.example.com/",
            ["Ante:Onboarding:SignUp"] = "JoinTenant",
            ["Ante:Onboarding:DefaultLocale"] = "nb-NO",
            ["Ante:Onboarding:AllowedLocales:0"] = "nb-NO",
            ["Ante:Onboarding:AllowedLocales:1"] = "en",
            ["Ante:Onboarding:Personas:0"] = "Owner",
            ["Ante:Onboarding:Legal:Folder"] = _directory,
            ["Ante:Onboarding:Secrets:Path"] = secrets,
            ["Ante:Onboarding:Secrets:SigningKeyFile"] = Path.Combine(_directory, "signing.pem"),
        });
    }

    void Because()
    {
        OnboardingConfiguration.Apply(_configuration, Substitute.For<IHostEnvironment>());
        _options = _configuration.GetSection("Ante").Get<AnteOptions>()!;
    }

    [Fact] void should_observe_the_applications_store() => Assert.Equal(["Shop"], _options.HostStores);
    [Fact] void should_let_the_contract_win_over_the_equivalent_setting() => Assert.Equal("https://shop.example.com/", _options.HostAppUrl);
    [Fact] void should_close_registration_when_people_only_join() => Assert.False(_options.Registration.Enabled);
    [Fact] void should_map_the_default_locale() => Assert.Equal("nb-NO", _options.DefaultLocale);
    [Fact] void should_map_the_allowed_locales() => Assert.Equal(["nb-NO", "en"], _options.SupportedLocales);
    [Fact] void should_read_legal_terms_from_files() => Assert.Equal(("Files", _directory), (_options.Legal.Source, _options.Legal.Files.Folder));
    [Fact] void should_keep_the_grantable_personas() => Assert.Equal(["Owner"], _options.Onboarding.Personas);
    [Fact] void should_read_the_signing_key_from_its_file() => Assert.Equal("signing-key-placeholder", _configuration["Ante:Invitations:Token:PrivateKeyPem"]);
    [Fact] void should_read_mounted_secrets_by_key_name() => Assert.Equal("verification-placeholder", _configuration["Ante:Invitations:Token:PublicKeyPem"]);
    [Fact] void should_publish_the_public_base_url() => Assert.Equal("https://onboard.shop.example.com", _configuration["Ante:Invitations:Token:PublicBaseUrl"]);
}
#endif
