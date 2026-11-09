// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;
using Ante.Organization.Registration.Review.for_RegistrationReviewReceiver.given;

namespace Ante.Organization.Registration.Review.for_RegistrationReviewReceiver.when_declining;

public class and_the_registration_is_held : a_review_decision
{
    void Establish() => History = [_held];

    Task Because() => Receiver.Decline(new("Not a fit for this offer"), Context);

    [Fact] void should_record_the_decline_with_the_contact_and_the_message() => ShouldHaveAppended<OrganizationRegistrationDeclineRecorded>(
        recorded => recorded.Email == "jane@example.com" && recorded.FirstName == "Jane" && recorded.LastName == "Doe" && recorded.Message == "Not a fit for this offer");

    [Fact] void should_not_complete_the_registration() => Log.DidNotReceive().Append(
        Arg.Any<EventSourceId>(),
        Arg.Any<OrganizationRegistrationRecorded>(),
        Arg.Any<EventStreamType>(),
        Arg.Any<EventStreamId>(),
        Arg.Any<EventSourceType>(),
        Arg.Any<Cratis.Execution.CorrelationId>(),
        Arg.Any<IEnumerable<string>>(),
        Arg.Any<Cratis.Chronicle.EventSequences.Concurrency.ConcurrencyScope>(),
        Arg.Any<DateTimeOffset?>(),
        Arg.Any<Cratis.Chronicle.Subject>());
}
#endif
