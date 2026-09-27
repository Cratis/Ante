// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.IdentityProviders;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_InviteExchangeProcessor.when_exchanging;

public class and_a_federation_marker_has_one_configured_provider : Specification
{
    readonly InvitationTokenFixture _fixture = new();
    bool _result;

    async Task Because()
    {
        var resolver = new IdentityProviderResolver(Options.Create(new IdentityProviderOptions
        {
            Providers = [new ConfiguredIdentityProvider { Name = "OnlyOIDC", Issuer = "https://oidc.example" }],
        }));
        _result = await InviteExchangeProcessor.TryStoreAcceptedInvitation(
            $"Bearer {_fixture.Token()}",
            new ExchangeInviteRequest("sub-1", "AuthenticationTypes.Federation", null, null),
            _fixture.Collection,
            resolver,
            _fixture.Validator());
    }

    [Fact] void should_accept_the_token() => Assert.True(_result);
    [Fact] void should_record_the_provider_that_later_requests_infer() =>
        _fixture.Collection.Received(1).ReplaceOneAsync(
            Arg.Any<FilterDefinition<AcceptedInvitation>>(),
            Arg.Is<AcceptedInvitation>(session => session.IdentityProvider == "OnlyOIDC"),
            Arg.Any<ReplaceOptions>(),
            Arg.Any<CancellationToken>());
}
#endif
