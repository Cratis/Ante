// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.IdentityProviders;
using Microsoft.Extensions.Options;

namespace Ante.IdentityProviders.for_IdentityProviderResolver.when_resolving;

public class and_an_issuer_was_reported : Specification
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

    void Because() => _result = _resolver.Resolve("https://accounts.google.com");

    [Fact] void should_resolve_to_the_configured_provider_name() => Assert.Equal("Google", _result);
}
#endif
