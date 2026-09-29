// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Issuing.for_InvitationTokenValidator.when_validating;

public class and_a_previous_key_signed_it : a_deployment_with_isolated_tokens
{
    ValidatedInvitationToken? _trusted;
    ValidatedInvitationToken? _untrusted;

    async Task Because()
    {
        var token = Token(Previous, "urn:cratis:ante:StudioLobby:Default", "urn:cratis:ante:StudioLobby:Default:lobby");
        _trusted = await ValidatorFor(Config(Previous)).Validate($"Bearer {token}");
        _untrusted = await ValidatorFor(Config()).Validate($"Bearer {token}");
    }

    [Fact] void should_accept_it_when_the_previous_key_is_configured() => Assert.NotNull(_trusted);
    [Fact] void should_refuse_it_otherwise() => Assert.Null(_untrusted);
}
#endif
