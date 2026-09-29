// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Issuing.for_InvitationTokenValidator.when_validating;

public class and_only_the_audience_was_configured_explicitly : a_deployment_with_isolated_tokens
{
    ValidatedInvitationToken? _result;

    async Task Because()
    {
        var config = new InvitationTokenConfig { Audience = "ingress", PrivateKeyPem = Current.ExportPkcs8PrivateKeyPem() };
        InvitationTokenIsolation.ApplyDefaults(config, Deployment);
        var token = Token(Current, issuer: null, audience: null, issuedAt: DateTime.UtcNow.AddHours(-1));
        _result = await ValidatorFor(config, new InvitationTokenUpgradeWindow(DateTimeOffset.UtcNow, TimeSpan.FromDays(7))).Validate($"Bearer {token}");
    }

    [Fact] void should_refuse_a_legacy_token_even_with_an_open_window() => Assert.Null(_result);
}
#endif
