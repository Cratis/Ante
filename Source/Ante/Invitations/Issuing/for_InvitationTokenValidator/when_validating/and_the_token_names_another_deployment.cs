// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Issuing.for_InvitationTokenValidator.when_validating;

public class and_the_token_names_another_deployment : a_deployment_with_isolated_tokens
{
    ValidatedInvitationToken? _result;

    async Task Because() => _result = await ValidatorFor(Config(), new InvitationTokenUpgradeWindow(DateTimeOffset.UtcNow.AddDays(1), TimeSpan.FromDays(7)))
        .Validate($"Bearer {Token(Current, "urn:cratis:ante:DirectLobby:Default", "urn:cratis:ante:DirectLobby:Default:lobby")}");

    [Fact] void should_refuse_it_even_with_a_shared_key_and_an_open_window() => Assert.Null(_result);
}
#endif
