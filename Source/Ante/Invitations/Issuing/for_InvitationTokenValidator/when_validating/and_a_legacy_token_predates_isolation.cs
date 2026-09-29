// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Issuing.for_InvitationTokenValidator.when_validating;

public class and_a_legacy_token_predates_isolation : a_deployment_with_isolated_tokens
{
    ValidatedInvitationToken? _whileOpen;
    ValidatedInvitationToken? _whenClosed;

    async Task Because()
    {
        var token = Token(Current, issuer: null, audience: null, issuedAt: DateTime.UtcNow.AddHours(-1));
        _whileOpen = await ValidatorFor(Config(), new InvitationTokenUpgradeWindow(DateTimeOffset.UtcNow, TimeSpan.FromDays(7))).Validate($"Bearer {token}");
        _whenClosed = await ValidatorFor(Config()).Validate($"Bearer {token}");
    }

    [Fact] void should_accept_it_while_the_upgrade_window_is_open() => Assert.NotNull(_whileOpen);
    [Fact] void should_refuse_it_once_the_window_is_closed() => Assert.Null(_whenClosed);
}
#endif
