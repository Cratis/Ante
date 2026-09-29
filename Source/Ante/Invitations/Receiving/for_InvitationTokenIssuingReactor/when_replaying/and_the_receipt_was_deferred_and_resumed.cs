// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Receiving.for_InvitationTokenIssuingReactor.when_replaying;

public class and_the_receipt_was_deferred_and_resumed : given.an_invitation_history
{
    void Establish()
    {
        History = [At(3, Receipt), At(4, new InvitationTokenIssuanceDeferred(3)), At(5, new InvitationTokenIssuanceResumed(3))];
        Published = [PublishedFor(5)];
        PublishingRecordsFor(0);
    }

    async Task Because()
    {
        await Reactor.OnReplay(Receipt, ContextAt(3));
        await Reactor.OnReplay(new InvitationTokenIssuanceResumed(3), ContextAt(5));
    }

    [Fact] void should_not_publish_a_second_token() => Assert.Equal(0, Publications);
}
#endif
