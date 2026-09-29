// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Issuing.for_InvitationTokenValidator.when_validating;

public class and_the_issuer_and_audience_were_configured_explicitly_and_the_token_carries_them : a_deployment_with_isolated_tokens
{
    ValidatedInvitationToken? _result;

    async Task Because() =>
        _result = await ValidatorFor(ConfigWithExplicit("https://lobby.example.com", "ingress"))
            .Validate($"Bearer {Token(Current, "https://lobby.example.com", "ingress")}");

    [Fact] void should_accept_it() => Assert.NotNull(_result);
}
#endif
