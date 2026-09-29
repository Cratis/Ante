// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.UserSetup.for_JoinTenantAcceptanceOutbox.given;

namespace Ante.Invitations.UserSetup.for_JoinTenantAcceptanceOutbox.when_forwarding;

/// <summary>
/// A status subscriber runs synchronously inside the push, and the owner filter does blocking reads there.
/// One that throws on the recorded status must not fail the reactor - that would redeliver the event - nor
/// stop the outbox append, and the watchers that did not fail still see the accepted status.
/// </summary>
public class and_a_subscriber_fails_when_recorded : a_forward_with_an_open_subscription
{
    Exception? _error;

    void Establish()
    {
        Watch();
        Watch(view =>
        {
            if (view.Status == UserSetupAcceptanceStatus.Recorded)
            {
                throw new InvalidOperationException("The subscriber's read failed");
            }
        });
    }

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(async () => await _scenario.Given.ForEventSource(EventSource).Events(_accepted));

    [Fact] void should_not_fail_the_reactor() => _error.ShouldBeNull();
    [Fact] void should_forward_to_the_outbox_once() => VerifyAppendedOnce();
    [Fact] void should_still_reach_accepted_for_the_watcher_that_did_not_fail() => _observed.ShouldContain(UserSetupAcceptanceStatus.Accepted);
}
#endif
