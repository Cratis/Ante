// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Receiving.for_IncomingInvitationSubscriptions.when_initializing;

public class and_an_audience_is_configured : given.a_lobby_with
{
    void Establish() => _options.Audience = "lobby-a";

    [Fact] void should_subscribe_once() => Assert.Single(_subscriptions);

    [Fact] void should_filter_the_subscription_to_the_audience() => Assert.Equal([("audience", "lobby-a")], _subscriptions[0].NamedTags.Select(tag => (tag.Name.Value, tag.Value)));
}
#endif
