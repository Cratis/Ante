// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.Events;

namespace Ante.Outbox.for_ReplyAudience.when_handling_a_request;

public class and_the_request_has_no_requester : Specification
{
    IReadOnlyList<NamedTag> _stamped = [];

    void Because()
    {
        using (ReplyAudience.Inbound(EventContext.Empty))
        {
            _stamped = EventTagResolver.Default.ResolveTags(new EventTagContext(
                new EventStoreName("Ante"), EventSequenceId.Log, "invitation-1", EventType.Unknown, new object(), []));
        }
    }

    [Fact] void should_stamp_nothing() => Assert.Empty(_stamped);
}
#endif
