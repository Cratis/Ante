// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Receiving.for_IncomingInvitationSubscriptions.when_initializing;

public class and_no_audience_is_configured : given.a_lobby_with
{
    Task Because() => Initialize();

    [Fact] void should_subscribe_once() => Assert.Single(_subscriptions);

    [Fact] void should_not_filter_on_tags() => Assert.Empty(_subscriptions[0].NamedTags);

    [Fact] void should_still_filter_on_the_event_types() => Assert.NotEmpty(_subscriptions[0].EventTypes);
}
#endif
