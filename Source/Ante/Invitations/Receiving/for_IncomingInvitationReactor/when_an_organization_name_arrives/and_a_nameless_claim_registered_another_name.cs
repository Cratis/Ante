// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;
using Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_organization_name_arrives.given;
using Ante.Organization.Names;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_organization_name_arrives;

public class and_a_nameless_claim_registered_another_name : a_name_delivery
{
    void Establish()
    {
        Claims = [new OrganizationNameClaim("registration-1", null!)];
        Histories["registration-1"] =
        [
            new OrganizationRegistrationCompleted("Northwind", "sub-1", "github", "Jane", MiddleName.NotSet, "Doe", "jane@example.com"),
        ];
    }

    Task Because() => Deliver(new OrganizationNameReleased("acme"));

    [Fact] void should_not_fail() => Assert.Null(Error);

    [Fact] void should_leave_the_claim_in_place() => Log.DidNotReceive().Append(
        (EventSourceId)"registration-1",
        Arg.Any<object>(),
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
