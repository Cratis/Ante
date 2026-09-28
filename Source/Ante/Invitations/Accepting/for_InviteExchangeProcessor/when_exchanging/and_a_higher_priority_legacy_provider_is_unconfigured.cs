// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.IdentityProviders;
using Cratis.Arc.Identity;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_InviteExchangeProcessor.when_exchanging;

public class and_a_higher_priority_legacy_provider_is_unconfigured : Specification
{
    readonly InvitationTokenFixture _fixture = new();
    bool _exchanged;
    string _requestProvider = string.Empty;

    async Task Because()
    {
        var resolver = new IdentityProviderResolver(Options.Create(new IdentityProviderOptions
        {
            Providers = [new ConfiguredIdentityProvider { Name = "GitHub" }],
        }));
        _exchanged = await InviteExchangeProcessor.TryStoreAcceptedInvitation(
            $"Bearer {_fixture.Token()}",
            new ExchangeInviteRequest("sub-1", "UnconfiguredIssuer", null, null),
            _fixture.Collection,
            resolver,
            _fixture.Validator(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<InviteExchangeBypassMiddleware>.Instance);
        _requestProvider = ForwardedIdentityProvider.Resolve(
            [
                new KeyValuePair<string, string>("iss", "UnconfiguredIssuer"),
                new KeyValuePair<string, string>("identity_provider", "GitHub"),
                new KeyValuePair<string, string>(MicrosoftIdentityPlatformClaims.IdentityProvider, "GitHub"),
            ],
            resolver);
    }

    [Fact] void should_keep_the_exchange_provider_unresolved() => Assert.True(_exchanged);
    [Fact] void should_not_replace_the_unconfigured_issuer_with_a_configured_lower_priority_claim() => Assert.Equal("UnconfiguredIssuer", _requestProvider);
    [Fact] void should_attribute_the_exchange_and_the_following_request_to_the_same_provider() =>
        _fixture.Collection.Received(1).ReplaceOneAsync(
            Arg.Any<FilterDefinition<AcceptedInvitation>>(),
            Arg.Is<AcceptedInvitation>(session => session.IdentityProvider == _requestProvider),
            Arg.Any<ReplaceOptions>(),
            Arg.Any<CancellationToken>());
}
#endif
