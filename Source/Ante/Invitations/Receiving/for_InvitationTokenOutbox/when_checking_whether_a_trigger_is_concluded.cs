// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;

namespace Ante.Invitations.Receiving.for_InvitationTokenOutbox;

public class when_checking_whether_a_trigger_is_concluded : Specification
{
    static AppendedEvent PublishedFor(params (string Sequence, ulong Number)[] deliveries) => new(
        EventContext.Empty with
        {
            Causation = [.. deliveries.Select(delivery => new Causation(
                DateTimeOffset.UnixEpoch,
                ReactorHandler.CausationType,
                new Dictionary<string, string>
                {
                    [ReactorHandler.CausationEventSequenceIdProperty] = delivery.Sequence,
                    [ReactorHandler.CausationEventSequenceNumberProperty] = delivery.Number.ToString(System.Globalization.CultureInfo.InvariantCulture),
                }.ToImmutableDictionary()))],
        },
        new InvitationTokenIssued(InvitationFlowType.JoinTenant, "token", DateTimeOffset.UnixEpoch));

    [Fact] void should_be_concluded_by_an_outcome_its_delivery_caused() =>
        Assert.True(InvitationTokenOutbox.IsConcluded([PublishedFor((EventSequenceId.Log.Value, 7))], [7]));

    [Fact] void should_be_concluded_by_an_outcome_any_of_its_deliveries_caused() =>
        Assert.True(InvitationTokenOutbox.IsConcluded([PublishedFor((EventSequenceId.Log.Value, 9))], [7, 9]));

    [Fact] void should_not_be_concluded_by_another_triggers_outcome() =>
        Assert.False(InvitationTokenOutbox.IsConcluded([PublishedFor((EventSequenceId.Log.Value, 3))], [7]));

    [Fact] void should_not_be_concluded_by_an_outcome_caused_from_an_inbox() =>
        Assert.False(InvitationTokenOutbox.IsConcluded([PublishedFor(($"{EventSequenceId.InboxPrefix}Host", 7))], [7]));

    [Fact] void should_only_consider_the_delivery_that_caused_the_outcome() =>
        Assert.False(InvitationTokenOutbox.IsConcluded([PublishedFor((EventSequenceId.Log.Value, 7), (EventSequenceId.Log.Value, 8))], [7]));

    [Fact] void should_not_be_concluded_by_an_outcome_without_reactor_causation() =>
        Assert.False(InvitationTokenOutbox.IsConcluded([new AppendedEvent(EventContext.Empty, new InvitationRejected(InvitationRejectionReason.InvalidRecipient))], [0]));
}
#endif
