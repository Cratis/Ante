// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Receiving.for_InvitationTokenIssuingReactor.when_replaying;

public class and_the_deferred_receipt_was_revoked : given.an_invitation_history
{
    void Establish()
    {
        History = [At(3, Receipt), At(4, new InvitationTokenIssuanceDeferred(3)), At(6, new InvitationRevocationReceived())];
        PublishingRecordsFor(0);
    }

    Task Because() => Reactor.OnReplay(Receipt, ContextAt(3));

    [Fact] void should_not_publish_a_token() => Assert.Equal(0, Publications);
}
#endif
