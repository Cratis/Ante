// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.IdentityProviders.for_IdentityProviderConfigurationWarnings.when_choosing_a_startup_warning;

public class and_provider_lists_differ : Specification
{
    [Theory]
    [InlineData("", UnattributableSignInWarning.NoProviders)]
    [InlineData("O", UnattributableSignInWarning.SingleIssuerlessProvider)]
    [InlineData("I", UnattributableSignInWarning.None)]
    [InlineData("OO", UnattributableSignInWarning.None)]
    [InlineData("OI", UnattributableSignInWarning.MultipleProvidersWithIssuer)]
    [InlineData("IO", UnattributableSignInWarning.MultipleProvidersWithIssuer)]
    [InlineData("II", UnattributableSignInWarning.MultipleProvidersWithIssuer)]
    void should_choose_the_warning_for_unattributable_sign_ins(string shape, UnattributableSignInWarning expected)
    {
        var options = new IdentityProviderOptions
        {
            Providers = [.. shape.Select((kind, index) => new ConfiguredIdentityProvider
            {
                Name = $"Provider{index}",
                Issuer = kind == 'I' ? $"https://issuer{index}.example" : string.Empty
            })]
        };

        IdentityProviderConfigurationWarnings.ChooseWarning(options).ShouldEqual(expected);
    }
}
#endif
