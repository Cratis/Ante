// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.IdentityProviders;
using Cratis.Arc.Identity;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_InviteExchangeProcessor.when_exchanging;

public class and_the_following_request_has_arcs_provider_claim : Specification
{
    readonly InvitationTokenFixture _fixture = new();
    InviteExchangeOutcome _exchanged;
    string _requestProvider = string.Empty;
    string _canonicalProvider = string.Empty;

    async Task Because()
    {
        var resolver = new IdentityProviderResolver(Options.Create(new IdentityProviderOptions
        {
            Providers =
            [
                new ConfiguredIdentityProvider { Name = "GitHub" },
                new ConfiguredIdentityProvider { Name = "OtherOAuth" },
            ],
        }));
        _exchanged = await InviteExchangeProcessor.TryStoreAcceptedInvitation(
            $"Bearer {_fixture.Token()}",
            new ExchangeInviteRequest("sub-1", "GitHub", "GitHub", null),
            _fixture.Collection,
            resolver,
            _fixture.Validator(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<InviteExchangeBypassMiddleware>.Instance,
            _fixture.Indexes);
        _requestProvider = ForwardedIdentityProvider.Resolve(
            [new KeyValuePair<string, string>(MicrosoftIdentityPlatformClaims.IdentityProvider, "GitHub")], resolver);
        _canonicalProvider = ForwardedIdentityProvider.Resolve(
            [
                new KeyValuePair<string, string>("urn:cratis:identity:provider-key", "GitHub"),
                new KeyValuePair<string, string>(MicrosoftIdentityPlatformClaims.IdentityProvider, "OtherOAuth"),
            ],
            resolver);
    }

    [Fact] void should_accept_the_exchange() => _exchanged.ShouldEqual(InviteExchangeOutcome.Accepted);
    [Fact] void should_record_the_provider_resolved_on_subsequent_requests() =>
        _fixture.Collection.Received(1).ReplaceOneAsync(
            Arg.Any<FilterDefinition<AcceptedInvitation>>(),
            Arg.Is<AcceptedInvitation>(session => session.IdentityProvider == _requestProvider && session.IdentityProvider == "GitHub"),
            Arg.Any<ReplaceOptions>(),
            Arg.Any<CancellationToken>());
    [Fact] void should_prefer_the_canonical_provider_over_arc_metadata() => Assert.Equal("GitHub", _canonicalProvider);
}
#endif
