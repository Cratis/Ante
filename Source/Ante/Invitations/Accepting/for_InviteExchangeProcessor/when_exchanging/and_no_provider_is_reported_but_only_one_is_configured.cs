// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.IdentityProviders;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_InviteExchangeProcessor.when_exchanging;

public class and_no_provider_is_reported_but_only_one_is_configured : Specification
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
            new ExchangeInviteRequest("sub-1", string.Empty, null, null),
            _fixture.Collection,
            resolver,
            _fixture.Validator());
    }

    [Fact] void should_accept_the_token() => Assert.True(_result);
    [Fact] void should_record_the_unambiguous_provider() =>
        _fixture.Collection.Received(1).ReplaceOneAsync(
            Arg.Any<FilterDefinition<AcceptedInvitation>>(),
            Arg.Is<AcceptedInvitation>(session => session.IdentityProvider == "OnlyOIDC"),
            Arg.Any<ReplaceOptions>(),
            Arg.Any<CancellationToken>());
}
#endif
