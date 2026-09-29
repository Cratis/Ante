// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Receiving.for_InvitationTokenIssuingReactor.when_replaying;

public class and_the_publication_was_lost : given.an_invitation_history
{
    void Establish()
    {
        History = [At(3, Receipt)];
        PublishingRecordsFor(3);
    }

    async Task Because()
    {
        await Reactor.OnReplay(Receipt, ContextAt(3));
        await Reactor.OnReplay(Receipt, ContextAt(3));
    }

    [Fact] void should_publish_the_token_once() => Assert.Equal(1, Publications);
}
#endif
