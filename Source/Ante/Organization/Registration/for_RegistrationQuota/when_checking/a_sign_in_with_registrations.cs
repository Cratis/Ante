// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;

namespace Ante.Organization.Registration.for_RegistrationQuota.when_checking;

public class a_sign_in_with_registrations : Specification
{
    protected static readonly DateTimeOffset Now = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
    protected static readonly RegistrationOwner Owner = new((RegistrationOwnerSubject)"sub-1", "github");
    protected IEventStore Store = null!;
    protected ImmutableList<AppendedEvent> History = [];

    void Establish()
    {
        Store = Substitute.For<IEventStore>();
        var log = Substitute.For<IEventLog>();
        Store.EventLog.Returns(log);
        log.GetForEventSourceIdAndEventTypes(RegistrationQuota.KeyFor(Owner), Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventSourceType>())
            .Returns(_ => Task.FromResult<IImmutableList<AppendedEvent>>(History));
    }

    protected static AppendedEvent ConsumedAt(DateTimeOffset occurred, EventSequenceNumber sequenceNumber) =>
        new(EventContext.Empty with { Occurred = occurred, SequenceNumber = sequenceNumber }, new RegistrationQuotaConsumed());
}
#endif
