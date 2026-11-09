// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Outbox.for_ReplyAudience.when_publishing;

public class and_only_the_history_names_the_requester : given.an_outbox_capturing_tags
{
    void Establish() => _history =
    [
        new(EventContext.Empty with { EventSourceId = "invitation-1", NamedTags = [From("operator-b")] }, new object()),
    ];

    Task Because() => Publish();

    [Fact] void should_address_the_reply_to_the_requester_found_in_the_history() => Assert.Equal(["operator-b"], OutboxAudiences);
}
#endif
