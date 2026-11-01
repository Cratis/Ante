// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Ante.Onboarding.for_OnboardingConfiguration;

public class when_running_in_development_mode : Specification
{
    string _directory = null!;
    string? _first;
    string? _second;
    string? _hostStore;

    void Establish() => _directory = Directory.CreateTempSubdirectory("ante-dev").FullName;

    void Because()
    {
        _first = Apply();
        _second = Apply();
        _hostStore = Configure().GetSection("Ante:HostStores:0").Value;
    }

    [Fact] void should_generate_a_signing_key() => Assert.StartsWith("-----BEGIN RSA PRIVATE KEY-----", _first);
    [Fact] void should_keep_the_same_key_on_the_data_volume() => Assert.Equal(_first, _second);
    [Fact] void should_use_one_host_store_without_a_fixture() => Assert.Equal("Application", _hostStore);

    [Fact]
    void should_refuse_a_production_environment()
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Production");
        Assert.Throws<OnboardingConfigurationInvalid>(() => OnboardingConfiguration.Apply(Build(), environment));
    }

    [Fact]
    void should_refuse_a_public_url()
    {
        var configuration = Build();
        configuration["Ante:Onboarding:PublicBaseUrl"] = "https://onboard.example.com";
        Assert.Throws<OnboardingConfigurationInvalid>(() => OnboardingConfiguration.Apply(configuration, Substitute.For<IHostEnvironment>()));
    }

    [Fact]
    void should_allow_loopback_http()
    {
        var configuration = Build();
        configuration["Ante:Onboarding:PublicBaseUrl"] = "http://localhost:8080";
        OnboardingConfiguration.Apply(configuration, Substitute.For<IHostEnvironment>());
        Assert.Equal("http://localhost:8080", configuration["Ante:Invitations:Token:PublicBaseUrl"]);
    }

    ConfigurationManager Build()
    {
        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ante:Onboarding:Enabled"] = "true",
            ["Ante:Onboarding:Development:Enabled"] = "true",
            ["Ante:Onboarding:Development:DataPath"] = _directory,
        });
        return configuration;
    }

    ConfigurationManager Configure()
    {
        var configuration = Build();
        OnboardingConfiguration.Apply(configuration, Substitute.For<IHostEnvironment>());
        return configuration;
    }

    string? Apply() => Configure()["Ante:Invitations:Token:PrivateKeyPem"];
}
#endif
