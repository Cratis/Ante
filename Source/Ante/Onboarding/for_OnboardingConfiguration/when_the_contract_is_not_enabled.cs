// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Ante.Onboarding.for_OnboardingConfiguration;

public class when_the_contract_is_not_enabled : Specification
{
    ConfigurationManager _configuration = null!;

    void Establish()
    {
        _configuration = new ConfigurationManager();
        _configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ante:HostStores:0"] = "Direct",
            ["Ante:Onboarding:HostStore"] = "Shop",
        });
    }

    void Because() => OnboardingConfiguration.Apply(_configuration, Substitute.For<IHostEnvironment>());

    [Fact] void should_leave_the_existing_settings_alone() => Assert.Equal("Direct", _configuration["Ante:HostStores:0"]);
    [Fact] void should_not_set_registration() => Assert.Null(_configuration["Ante:Registration:Enabled"]);
}
#endif
