// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Outbox.for_ReplyAudience.when_publishing;

public class and_nothing_names_a_requester : given.an_outbox_capturing_tags
{
    Task Because() => Publish();

    [Fact] void should_leave_the_reply_unaddressed() => Assert.Empty(OutboxAudiences);
}
#endif
