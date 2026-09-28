// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Security.Claims;
using Ante.IdentityProviders;
using Ante.Invitations.for_query_access;
using Ante.Organization.Registration;
using Cratis.Chronicle.EventSequences;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_recovering_an_attested_acceptance.given;

public class a_committed_owner : Specification
{
    protected readonly InvitationId Id = InvitationId.New();
    protected SignedInIdentity Identity = null!;
    protected IEventStore Store = null!;
    protected string Subject = "CaseSensitive";
    protected DateTime SessionExpiry = DateTime.UtcNow.AddMinutes(-1);

    void Establish()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(_ => new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("urn:cratis:identity:subject", Subject),
            new Claim("urn:cratis:identity:provider-key", "canonical-key"),
            new Claim("urn:cratis:identity:issuer", "https://identity.example"),
        ],
        "proxy")) });
        var expired = new AttestedInvitationSession(
            "tx",
            "lobby",
            Id,
            InvitationFlowType.JoinTenant,
            "canonical-key",
            "https://identity.example",
            Subject,
            ["jti"],
            SessionExpiry);
        Identity = new SignedInIdentity(
            accessor,
            Substitute.For<IMongoCollection<AcceptedInvitation>>(),
            Substitute.For<IIdentityProviderResolver>(),
            Options.Create(new InvitationExchangeConfig { Mode = InvitationExchangeMode.Attested, Attestation = new() { LobbyScope = "lobby" } }),
            QueryCollections.With(expired));
        Store = Substitute.For<IEventStore>();
        var log = Substitute.For<IEventLog>();
        Store.EventLog.Returns(log);
        log.GetForEventSourceIdAndEventTypes(
            Arg.Any<EventSourceId>(),
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventSourceType>())
            .Returns(_ => Task.FromResult<IImmutableList<AppendedEvent>>([
                new AppendedEvent(EventContext.Empty, new InvitedAcceptanceOwnerRecorded(
                    "lobby",
                    "canonical-key",
                    "https://identity.example",
                    (RegistrationOwnerSubject)"CaseSensitive")),
            ]));
    }
}
#endif
