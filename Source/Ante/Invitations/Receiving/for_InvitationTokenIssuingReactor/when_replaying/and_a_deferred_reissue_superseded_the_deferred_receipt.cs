// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Receiving.for_InvitationTokenIssuingReactor.when_replaying;

public class and_a_deferred_reissue_superseded_the_deferred_receipt : given.an_invitation_history
{
    void Establish()
    {
        History =
        [
            At(3, Receipt),
            At(4, new InvitationTokenIssuanceDeferred(3)),
            At(6, new InvitationReissueReceived(12, "Host")),
            At(7, new InvitationTokenIssuanceDeferred(6)),
            At(8, new InvitationTokenIssuanceResumed(6)),
        ];
        Published = [PublishedFor(8)];
        PublishingRecordsFor(0);
    }

    async Task Because()
    {
        await Reactor.OnReplay(Receipt, ContextAt(3));
        await Reactor.OnReplay(new InvitationReissueReceived(12, "Host"), ContextAt(6));
        await Reactor.OnReplay(new InvitationTokenIssuanceResumed(6), ContextAt(8));

        // A resumption of the receipt - had one been recorded - must not issue for the superseded receipt either.
        await Reactor.OnReplay(new InvitationTokenIssuanceResumed(3), ContextAt(9));
    }

    [Fact] void should_not_publish_anything() => Assert.Equal(0, Publications);
}
#endif
