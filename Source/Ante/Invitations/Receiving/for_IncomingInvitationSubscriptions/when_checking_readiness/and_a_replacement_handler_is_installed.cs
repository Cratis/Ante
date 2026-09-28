// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Receiving.for_IncomingInvitationSubscriptions.when_checking_readiness;

public class and_a_replacement_handler_is_installed : given.a_registered_inbox
{
    bool _readyBeforeReconnect;
    bool _readyAfterReconnect;
    IReactorHandler _replacement = null!;

    async Task Because()
    {
        _readyBeforeReconnect = await _routing.IsReady(_options);
        _original.CancellationToken.Returns(_ => throw new ObjectDisposedException("Original handler"));
        _replacement = Substitute.For<IReactorHandler>();
        _replacement.GetState().Returns(Task.FromResult(ActiveState()));
        _reactors.GetHandlerById(IncomingInvitationSubscriptions.ReactorIdFor("StudioAdmin")).Returns(_replacement);
        _readyAfterReconnect = await _routing.IsReady(_options);
    }

    [Fact] void should_have_been_ready_before_reconnect() => Assert.True(_readyBeforeReconnect);
    [Fact] void should_recover_readiness_without_accessing_the_disposed_handler() => Assert.True(_readyAfterReconnect);
    [Fact] async Task should_probe_the_replacement_handler() => await _replacement.Received(1).GetState();
    [Fact] async Task should_not_probe_the_old_handler_again() => await _original.Received(1).GetState();
}
#endif
