// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Issuing.for_InvitationTokenValidator.when_validating;

public class and_no_window_was_ever_opened : a_deployment_with_isolated_tokens
{
    ValidatedInvitationToken? _result;

    async Task Because()
    {
        var window = new InvitationTokenUpgradeWindow(DateTimeOffset.MinValue, TimeSpan.Zero);
        var token = Token(Current, issuer: null, audience: null, issuedAt: DateTime.UtcNow.AddHours(-1));
        _result = await ValidatorFor(Config(), window).Validate($"Bearer {token}");
    }

    [Fact] void should_refuse_it() => Assert.Null(_result);
}
#endif
