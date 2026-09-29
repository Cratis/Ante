// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Issuing.for_InvitationTokenValidator.when_validating;

public class and_a_legacy_token_was_issued_within_the_rollout_grace : a_deployment_with_isolated_tokens
{
    ValidatedInvitationToken? _result;

    async Task Because()
    {
        var now = DateTimeOffset.UtcNow;
        var window = new InvitationTokenUpgradeWindow(now.AddMinutes(-10), TimeSpan.FromDays(7));
        var token = Token(Current, issuer: null, audience: null, issuedAt: DateTime.UtcNow.AddMinutes(-1));
        _result = await ValidatorFor(Config(), window).Validate($"Bearer {token}");
    }

    [Fact] void should_accept_it() => Assert.NotNull(_result);
}
#endif
