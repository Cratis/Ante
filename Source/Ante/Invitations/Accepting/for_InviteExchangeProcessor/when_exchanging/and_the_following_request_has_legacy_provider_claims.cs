// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.IdentityProviders;
using Cratis.Arc.Identity;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_InviteExchangeProcessor.when_exchanging;

public class and_the_following_request_has_legacy_provider_claims : Specification
{
    [Theory]
    [InlineData("identity_provider")]
    [InlineData("http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider")]
    async Task should_resolve_the_same_provider_at_exchange_and_on_the_following_request(string claimType)
    {
        var fixture = new InvitationTokenFixture();
        var resolver = new IdentityProviderResolver(Options.Create(new IdentityProviderOptions
        {
            Providers =
            [
                new ConfiguredIdentityProvider { Name = "GitHub" },
                new ConfiguredIdentityProvider { Name = "OtherOAuth" },
            ],
        }));
        var exchanged = await InviteExchangeProcessor.TryStoreAcceptedInvitation(
            $"Bearer {fixture.Token()}",
            new ExchangeInviteRequest("sub-1", "GitHub", null, null),
            fixture.Collection,
            resolver,
            fixture.Validator(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<InviteExchangeBypassMiddleware>.Instance);
        var requestProvider = ForwardedIdentityProvider.Resolve(
            [
                new KeyValuePair<string, string>(claimType, "GitHub"),
                new KeyValuePair<string, string>(MicrosoftIdentityPlatformClaims.IdentityProvider, "OtherOAuth"),
            ],
            resolver);

        Assert.True(exchanged);
        Assert.Equal("GitHub", requestProvider);
        await fixture.Collection.Received(1).ReplaceOneAsync(
            Arg.Any<FilterDefinition<AcceptedInvitation>>(),
            Arg.Is<AcceptedInvitation>(session => session.IdentityProvider == requestProvider),
            Arg.Any<ReplaceOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    void should_prefer_identity_provider_over_the_schema_claim_and_arc_metadata()
    {
        var resolver = new IdentityProviderResolver(Options.Create(new IdentityProviderOptions
        {
            Providers =
            [
                new ConfiguredIdentityProvider { Name = "GitHub" },
                new ConfiguredIdentityProvider { Name = "OtherOAuth" },
            ],
        }));
        var provider = ForwardedIdentityProvider.Resolve(
            [
                new KeyValuePair<string, string>("identity_provider", "GitHub"),
                new KeyValuePair<string, string>("http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider", "OtherOAuth"),
                new KeyValuePair<string, string>(MicrosoftIdentityPlatformClaims.IdentityProvider, "OtherOAuth"),
            ],
            resolver);

        Assert.Equal("GitHub", provider);
    }
}
#endif
