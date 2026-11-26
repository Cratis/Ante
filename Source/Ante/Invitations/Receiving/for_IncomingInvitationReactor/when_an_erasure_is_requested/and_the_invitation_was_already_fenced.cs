// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_erasure_is_requested.given;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_erasure_is_requested;

public class and_the_invitation_was_already_fenced : an_erasure_request
{
    void Establish() => History = [Recorded(new InvitationRevocationReceived(), 4)];

    Task Because() => Deliver();

    [Fact] void should_not_fence_it_again() => ShouldHaveFencedTheInvitation(0);

    [Fact] void should_still_remove_the_mongo_documents() => Documents.Received(1).Erase(Arg.Any<InvitationId>());

    [Fact] void should_still_delete_the_key() =>
        Pii.Received(1).DeleteEncryptionKeyFor(Arg.Any<Cratis.Chronicle.Compliance.EncryptionKeyIdentifier>());

    [Fact] void should_acknowledge() => ShouldHaveAcknowledged(1);
}
#endif
