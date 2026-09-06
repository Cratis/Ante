// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.IdentityProviders;
using Microsoft.Extensions.Options;

namespace Ante.IdentityProviders.for_IdentityProviderResolver.when_resolving;

public class and_the_provider_was_never_identified : Specification
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

    // Nothing reported, and more than one configured provider issues no issuer of its own - genuinely
    // ambiguous, since either provider is created without one of the actual reference (GitHub is here
    // the only issuerless provider in the other specs; this exercises the "several" branch instead).
    void Because() => _result = _resolver.ResolveFrom([null, "AuthenticationTypes.Federation"]);

    [Fact]
    void should_resolve_to_an_empty_string() => Assert.Equal(string.Empty, _result);
}
#endif
