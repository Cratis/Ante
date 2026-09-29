// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Issuing.for_InvitationTokenValidator.when_validating;

public class and_a_token_carries_only_an_issuer : a_deployment_with_isolated_tokens
{
    ValidatedInvitationToken? _result;

    async Task Because()
    {
        var window = new InvitationTokenUpgradeWindow(DateTimeOffset.UtcNow, TimeSpan.FromDays(7));
        var token = Token(Current, issuer: Config().Issuer, audience: null, issuedAt: DateTime.UtcNow.AddHours(-1));
        _result = await ValidatorFor(Config(), window).Validate($"Bearer {token}");
    }

    [Fact] void should_refuse_it_even_while_the_upgrade_window_is_open() => Assert.Null(_result);
}
#endif
