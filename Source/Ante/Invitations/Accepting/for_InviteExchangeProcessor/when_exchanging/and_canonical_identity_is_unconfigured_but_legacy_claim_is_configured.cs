// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.IdentityProviders;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_InviteExchangeProcessor.when_exchanging;

public class and_canonical_identity_is_unconfigured_but_legacy_claim_is_configured : Specification
{
    readonly InvitationTokenFixture _fixture = new();
    InviteExchangeOutcome _exchanged;
    string _requestProvider = string.Empty;

    async Task Because()
    {
        var resolver = new IdentityProviderResolver(Options.Create(new IdentityProviderOptions
        {
            Providers = [new ConfiguredIdentityProvider { Name = "LegacyOAuth" }]
        }));
        _exchanged = await InviteExchangeProcessor.TryStoreAcceptedInvitation(
            $"Bearer {_fixture.Token()}",
            new ExchangeInviteRequest("sub-1", "unconfigured-key", "unconfigured-key", "https://unconfigured.example"),
            _fixture.Collection,
            resolver,
            _fixture.Validator(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<InviteExchangeBypassMiddleware>.Instance,
            _fixture.Indexes);
        _requestProvider = ForwardedIdentityProvider.Resolve(
        [
            new("urn:cratis:identity:provider-key", "unconfigured-key"),
            new("urn:cratis:identity:issuer", "https://unconfigured.example"),
            new("identity_provider", "LegacyOAuth"),
        ],
        resolver);
    }

    [Fact] void should_accept_the_unconfigured_canonical_provider_without_substituting_the_legacy_provider() => _exchanged.ShouldEqual(InviteExchangeOutcome.Accepted);
    [Fact] void should_resolve_the_same_provider_on_following_requests() => _requestProvider.ShouldEqual("unconfigured-key");
    [Fact] async Task should_record_that_provider_at_exchange() => await _fixture.Collection.Received(1).ReplaceOneAsync(
        Arg.Any<FilterDefinition<AcceptedInvitation>>(),
        Arg.Is<AcceptedInvitation>(session => session.IdentityProvider == _requestProvider),
        Arg.Any<ReplaceOptions>(),
        Arg.Any<CancellationToken>());
}
#endif
