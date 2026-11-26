// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_erasure_is_requested.given;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_erasure_is_requested;

public class and_the_invitation_is_pending : an_erasure_request
{
    void Establish() => History = [];

    Task Because() => Deliver();

    [Fact] void should_fence_the_invitation_against_acceptance() => ShouldHaveFencedTheInvitation(1);

    [Fact] void should_remove_the_mongo_documents_of_the_invitation() =>
        Documents.Received(1).Erase(new InvitationId(Guid.Parse(Id.Value)));

    [Fact] void should_delete_the_encryption_key_of_the_compliance_subject() =>
        Pii.Received(1).DeleteEncryptionKeyFor(Arg.Any<Cratis.Chronicle.Compliance.EncryptionKeyIdentifier>());

    [Fact] void should_acknowledge_once_everything_is_gone() => ShouldHaveAcknowledged(1);
}
#endif
