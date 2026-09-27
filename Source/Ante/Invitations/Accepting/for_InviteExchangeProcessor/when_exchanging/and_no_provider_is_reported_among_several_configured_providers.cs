// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.IdentityProviders;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.Accepting.for_InviteExchangeProcessor.when_exchanging;

public class and_no_provider_is_reported_among_several_configured_providers : Specification
{
    readonly InvitationTokenFixture _fixture = new();
    bool _result;

    async Task Because()
    {
        var resolver = new IdentityProviderResolver(Options.Create(new IdentityProviderOptions
        {
            Providers =
            [
                new ConfiguredIdentityProvider { Name = "FirstOIDC", Issuer = "https://first.example" },
                new ConfiguredIdentityProvider { Name = "SecondOIDC", Issuer = "https://second.example" },
            ],
        }));
        _result = await InviteExchangeProcessor.TryStoreAcceptedInvitation(
            $"Bearer {_fixture.Token()}",
            new ExchangeInviteRequest("sub-1", "AuthenticationTypes.Federation", null, null),
            _fixture.Collection,
            resolver,
            _fixture.Validator());
    }

    [Fact] void should_reject_the_exchange() => Assert.False(_result);
    [Fact] void should_not_record_a_session() => Assert.Empty(_fixture.Collection.ReceivedCalls());
}
#endif
