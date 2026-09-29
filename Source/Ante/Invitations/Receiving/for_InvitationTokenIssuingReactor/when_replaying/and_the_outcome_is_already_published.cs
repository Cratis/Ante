// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Receiving.for_InvitationTokenIssuingReactor.when_replaying;

public class and_the_outcome_is_already_published : given.an_invitation_history
{
    void Establish()
    {
        History = [At(3, Receipt)];
        Published = [PublishedFor(3)];
        PublishingRecordsFor(3);
    }

    Task Because() => Reactor.OnReplay(Receipt, ContextAt(3));

    [Fact] void should_not_publish_anything() => Assert.Equal(0, Publications);
}
#endif
