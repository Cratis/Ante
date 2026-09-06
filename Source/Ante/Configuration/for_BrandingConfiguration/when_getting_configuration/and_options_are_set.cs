// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Microsoft.Extensions.Options;

namespace Ante.Configuration.for_BrandingConfiguration.when_getting_configuration;

public class and_options_are_set : Specification
{
    BrandingConfiguration _result = null!;

    void Because() =>
        _result = BrandingConfiguration.GetConfiguration(Options.Create(new AnteOptions
        {
            LogoUrl = "https://example.com/logo.svg",
            CustomCssUrl = "https://example.com/custom.css",
        }));

    [Fact] void should_carry_the_logo_url() => Assert.Equal("https://example.com/logo.svg", _result.LogoUrl);
    [Fact] void should_carry_the_custom_css_url() => Assert.Equal("https://example.com/custom.css", _result.CustomCssUrl);
}
#endif
