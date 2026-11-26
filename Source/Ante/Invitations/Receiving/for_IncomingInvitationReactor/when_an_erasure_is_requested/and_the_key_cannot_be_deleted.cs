// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_erasure_is_requested.given;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_erasure_is_requested;

public class and_the_key_cannot_be_deleted : an_erasure_request
{
    Exception? _failure;

    void Establish() => Pii.DeleteEncryptionKeyFor(Arg.Any<Cratis.Chronicle.Compliance.EncryptionKeyIdentifier>())
        .Returns(Task.FromException(new InvalidOperationException("The key store is unavailable.")));

    async Task Because() => _failure = await Cratis.Specifications.Catch.Exception(Deliver);

    [Fact] void should_fail_the_delivery_so_it_is_retried() => _failure.ShouldBeOfExactType<InvalidOperationException>();

    [Fact] void should_not_acknowledge() => ShouldHaveAcknowledged(0);
}
#endif
