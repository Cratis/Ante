// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.Events;

namespace Ante.Outbox.for_ReplyAudience.when_handling_a_request;

public class and_the_request_names_its_requester : Specification
{
    IReadOnlyList<NamedTag> _stamped = [];

    void Because()
    {
        var inbound = EventContext.Empty with { NamedTags = [new(new TagName("from"), "operator-a")] };
        using (ReplyAudience.Inbound(inbound))
        {
            _stamped = Resolve();
        }
    }

    [Fact] void should_stamp_the_requester_on_what_is_appended() => Assert.Equal(["operator-a"], _stamped.Where(tag => tag.Name.Value == "from").Select(tag => tag.Value));

    static IReadOnlyList<NamedTag> Resolve() => EventTagResolver.Default.ResolveTags(new EventTagContext(
        new EventStoreName("Ante"), EventSequenceId.Log, "invitation-1", EventType.Unknown, new object(), []));
}
#endif
