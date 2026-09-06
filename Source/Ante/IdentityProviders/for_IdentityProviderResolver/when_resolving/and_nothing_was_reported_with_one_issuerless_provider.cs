// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.IdentityProviders;
using Microsoft.Extensions.Options;

namespace Ante.IdentityProviders.for_IdentityProviderResolver.when_resolving;

public class and_nothing_was_reported_with_one_issuerless_provider : Specification
{
    IdentityProviderResolver _resolver = null!;
    string _result = string.Empty;

    void Establish()
    {
        var options = new IdentityProviderOptions
        {
            Providers =
            [
                new() { Name = "GitHub", Issuer = string.Empty },
                new() { Name = "Google", Issuer = "https://accounts.google.com" },
            ],
        };
        _resolver = new(Options.Create(options));
    }

    void Because() => _result = _resolver.Resolve(null);

    [Fact]
    void should_attribute_it_to_the_only_provider_that_issues_no_issuer() => Assert.Equal("GitHub", _result);
}
#endif
