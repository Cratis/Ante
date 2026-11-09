// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Onboarding.for_OnboardingConfiguration;

public class when_the_contract_is_inconsistent : Specification
{
    [Fact]
    void should_refuse_an_unknown_sign_up() =>
        Assert.Throws<OnboardingConfigurationInvalid>(() => OnboardingConfiguration.Validate(new() { HostStore = "Shop", SignUp = "Anyone" }));

    [Fact]
    void should_refuse_an_unknown_invitation_type() =>
        Assert.Throws<OnboardingConfigurationInvalid>(() => OnboardingConfiguration.Validate(new() { HostStore = "Shop", InvitationTypes = ["Other"] }));

    [Fact]
    void should_refuse_a_missing_host_store_outside_development() =>
        Assert.Throws<OnboardingConfigurationInvalid>(() => OnboardingConfiguration.Validate(new()));

    [Fact]
    void should_accept_no_host_store_in_development() =>
        OnboardingConfiguration.Validate(new() { Development = new() { Enabled = true } });
}
#endif
