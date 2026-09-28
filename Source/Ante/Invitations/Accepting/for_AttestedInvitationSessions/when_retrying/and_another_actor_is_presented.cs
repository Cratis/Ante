// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting.for_AttestedInvitationSessions.given;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_AttestedInvitationSessions.when_retrying;

public class and_another_actor_is_presented : a_recorded_completion
{
    async Task Because() => Outcome = await Sessions.Retry(Stage, Assertion with { ProviderSubject = "other" });
    [Fact] void should_refuse_the_conflicting_actor() => Assert.Equal(AttestedSessionOutcome.Rejected, Outcome);
    [Fact] async Task should_not_claim_its_assertion() => await Collection.DidNotReceiveWithAnyArgs().UpdateOneAsync(
        default, default, default, default);
}
#endif
