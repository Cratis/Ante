// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Outbox.for_ReplyAudience.when_publishing;

public class and_the_request_names_its_requester : given.an_outbox_capturing_tags
{
    void Establish()
    {
        _options.OperatorAudience = "fallback";
        _trigger = _trigger with { NamedTags = [From("operator-a")] };
    }

    Task Because() => Publish();

    [Fact] void should_address_the_reply_to_the_requester() => Assert.Equal(["operator-a"], OutboxAudiences);
}
#endif
