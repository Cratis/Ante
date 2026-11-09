// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Outbox.for_ReplyAudience.when_publishing;

public class and_no_requester_is_named_but_a_fallback_is_configured : given.an_outbox_capturing_tags
{
    void Establish() => _options.OperatorAudience = "fallback";

    Task Because() => Publish();

    [Fact] void should_address_the_reply_to_the_fallback() => Assert.Equal(["fallback"], OutboxAudiences);
}
#endif
