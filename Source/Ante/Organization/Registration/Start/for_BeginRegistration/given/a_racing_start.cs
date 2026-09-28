// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Security.Claims;
using Ante.IdentityProviders;
using Ante.Invitations;
using Microsoft.AspNetCore.Http;

namespace Ante.Organization.Registration.Start.for_BeginRegistration.given;

public class a_racing_start : Specification
{
    protected readonly InvitationId Id = InvitationId.New();
    protected IHttpContextAccessor Accessor = null!;
    protected IIdentityProviderResolver Resolver = null!;
    protected IEventStore Store = null!;
    protected int HistoryReads;
    protected string WinningSubject = "subject-1";

    void Establish()
    {
        Accessor = Substitute.For<IHttpContextAccessor>();
        Accessor.HttpContext.Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "subject-1")], "proxy")),
        });
        Resolver = Substitute.For<IIdentityProviderResolver>();
        Resolver.ResolveFrom(Arg.Any<IEnumerable<string?>>()).Returns("github");
        Store = Substitute.For<IEventStore>();
        var log = Substitute.For<IEventLog>();
        Store.EventLog.Returns(log);
        log.GetForEventSourceIdAndEventTypes(
            Arg.Any<EventSourceId>(),
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventSourceType>())
            .Returns(_ => Task.FromResult<IImmutableList<AppendedEvent>>(++HistoryReads < 3
                ? []
                : [new AppendedEvent(EventContext.Empty, new RegistrationStarted((RegistrationOwnerSubject)WinningSubject, "github"))]));
        log.Append((EventSourceId)Id.Value.ToString("D"), Arg.Any<object>())
            .Returns(Task.FromResult(new AppendResult
            {
                ConstraintViolations = [new ConstraintViolation(
                    typeof(RegistrationStarted).GetEventType().Id,
                    EventSequenceNumber.First,
                    ConstraintType.UniqueEventType,
                    "OneRegistrationStart",
                    "This registration belongs to another sign-in.",
                    [])],
            }));
    }
}
#endif
