// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Issuing.for_InvitationTokenValidator.when_validating;

public class and_many_tokens_are_validated_at_once : a_deployment_with_isolated_tokens
{
    const int Tokens = 200;

    int _refused;

    async Task Because()
    {
        var validator = ValidatorFor(Config(), new InvitationTokenUpgradeWindow(DateTimeOffset.UtcNow, TimeSpan.FromDays(7)));
        var current = Enumerable.Range(0, Tokens).Select(_ => Token(Current, "urn:cratis:ante:StudioLobby:Default", "urn:cratis:ante:StudioLobby:Default:lobby"));
        var legacy = Enumerable.Range(0, Tokens).Select(_ => Token(Current, issuer: null, audience: null, issuedAt: DateTime.UtcNow.AddHours(-1)));

        await Parallel.ForEachAsync(
            current.Concat(legacy).ToArray(),
            new ParallelOptions { MaxDegreeOfParallelism = 16 },
            async (token, _) =>
            {
                if (await validator.Validate($"Bearer {token}") is null)
                {
                    Interlocked.Increment(ref _refused);
                }
            });
    }

    [Fact] void should_accept_every_one_of_them() => Assert.Equal(0, _refused);
}
#endif
