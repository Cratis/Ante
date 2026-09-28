// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Receiving.for_IncomingInvitationSubscriptions.when_checking_readiness;

public class and_no_handler_is_available_after_reconnect : given.a_registered_inbox
{
    bool _readyBeforeReconnect;
    bool _readyAfterReconnect;

    async Task Because()
    {
        _readyBeforeReconnect = await _routing.IsReady(_options);
        _original.CancellationToken.Returns(_ => throw new ObjectDisposedException("Original handler"));
        _reactors.GetHandlerById(IncomingInvitationSubscriptions.ReactorIdFor("StudioAdmin"))
            .Returns(_ => throw new UnknownReactorId(IncomingInvitationSubscriptions.ReactorIdFor("StudioAdmin")));
        _readyAfterReconnect = await _routing.IsReady(_options);
    }

    [Fact] void should_have_been_ready_before_reconnect() => Assert.True(_readyBeforeReconnect);
    [Fact] void should_not_be_ready_without_a_current_handler() => Assert.False(_readyAfterReconnect);
    [Fact] async Task should_not_probe_the_disposed_handler_again() => await _original.Received(1).GetState();
}
#endif
