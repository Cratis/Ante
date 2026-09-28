// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Arc.Identity;
using Microsoft.Extensions.Options;

namespace Ante.IdentityProviders.for_AuthProxySignInReport.when_resolving;

public class and_exchange_and_forwarded_requests_describe_the_same_sign_in : Specification
{
    public static TheoryData<int, int> Cases
    {
        get
        {
            var cases = new TheoryData<int, int>();
            foreach (var shape in Enumerable.Range(0, 6))
            {
                foreach (var mask in Enumerable.Range(0, 64))
                {
                    cases.Add(shape, mask);
                }
            }

            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    void should_resolve_identically_using_the_real_provider_resolver(int shape, int mask)
    {
        var canonical = (mask & 1) != 0;
        var configuredKey = (mask & 2) != 0;
        var hasIss = (mask & 4) != 0;
        var hasIdentityProvider = (mask & 8) != 0;
        var hasSchemaProvider = (mask & 16) != 0;
        var hasFederationMarker = (mask & 32) != 0;
        var options = new IdentityProviderOptions
        {
            Providers = shape switch
            {
                0 => [],
                1 => [new ConfiguredIdentityProvider { Name = "Workforce" }],
                2 => [new ConfiguredIdentityProvider { Name = "Workforce", Issuer = "https://workforce.example" }],
                3 => [new ConfiguredIdentityProvider { Name = "Workforce" }, new ConfiguredIdentityProvider { Name = "LegacyOAuth" }],
                4 => [new ConfiguredIdentityProvider { Name = "Workforce", Issuer = "https://workforce.example" }, new ConfiguredIdentityProvider { Name = "LegacyOIDC", Issuer = "https://legacy.example" }],
                _ => [new ConfiguredIdentityProvider { Name = "Workforce", Issuer = "https://workforce.example" }, new ConfiguredIdentityProvider { Name = "LegacyOAuth" }],
            }
        };
        var resolver = new IdentityProviderResolver(Options.Create(options));
        string? key = null;
        string? issuer = null;
        if (canonical)
        {
            key = configuredKey ? "Workforce" : "unconfigured-key";
            issuer = configuredKey ? "https://workforce.example" : "https://unconfigured.example";
        }
        var iss = hasIss ? "https://legacy.example" : null;
        var legacy = hasIdentityProvider ? "LegacyOAuth" : null;
        var schema = hasSchemaProvider ? "Workforce" : null;
        var authenticationType = hasFederationMarker ? "AuthenticationTypes.Federation" : string.Empty;

        // InviteCompletion.ExchangeInvite chooses exactly one legacy value by first presence;
        // with canonical identity its body instead sends key, normalized issuer and key again.
        var exchangeBody = new AuthProxySignInReport(key, issuer, canonical ? key : iss ?? legacy ?? schema ?? authenticationType);
        var forwardedClaims = new List<KeyValuePair<string, string>>();
        if (key is not null) forwardedClaims.Add(new("urn:cratis:identity:provider-key", key));
        if (issuer is not null) forwardedClaims.Add(new("urn:cratis:identity:issuer", issuer));
        if (iss is not null) forwardedClaims.Add(new("iss", iss));
        if (legacy is not null) forwardedClaims.Add(new("identity_provider", legacy));
        if (schema is not null) forwardedClaims.Add(new("http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider", schema));
        forwardedClaims.Add(new(MicrosoftIdentityPlatformClaims.IdentityProvider, canonical ? key! : authenticationType));

        var exchangeProvider = resolver.ResolveFrom(exchangeBody.Candidates());
        var requestProvider = ForwardedIdentityProvider.Resolve(forwardedClaims, resolver);
        Assert.Equal(exchangeProvider, requestProvider);
        if (canonical && !configuredKey)
        {
            Assert.Equal("unconfigured-key", requestProvider);
        }
    }
}
#endif
