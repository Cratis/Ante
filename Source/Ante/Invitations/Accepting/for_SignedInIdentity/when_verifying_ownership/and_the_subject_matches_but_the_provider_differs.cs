// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Ante.IdentityProviders;
using Ante.Invitations.for_query_access;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_SignedInIdentity.when_verifying_ownership;

public class and_the_subject_matches_but_the_provider_differs : Specification
{
    readonly InvitationId _id = InvitationId.New();
    SignedInIdentity _identity = null!;
    IMongoCollection<AcceptedInvitation> _sessions = null!;

    void Establish()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, "shared-subject"),
                new Claim("iss", "provider-b")],
                "proxy")),
        });
        var resolver = Substitute.For<IIdentityProviderResolver>();
        resolver.ResolveFrom(Arg.Any<IEnumerable<string?>>()).Returns("provider-b");
        var session = new AcceptedInvitation("shared-subject", "provider-a", _id, InvitationFlowType.JoinTenant, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(20));
        _sessions = QueryCollections.With(session);
        _identity = new SignedInIdentity(accessor, _sessions, resolver);
    }

    [Fact] void should_not_own_the_invitation() => Assert.False(_identity.IsVerifiedOwnerOf(_id));
    [Fact] void should_not_find_a_current_invitation() => Assert.Equal(InvitationId.NotSet, _identity.CurrentInvitationId());
    [Fact] void should_filter_sessions_by_the_forwarded_subject()
    {
        _identity.IsVerifiedOwnerOf(_id);
        var filter = _sessions.ReceivedCalls().First(call => call.GetMethodInfo().Name == "FindSync").GetArguments()[0] as FilterDefinition<AcceptedInvitation>;
        var rendered = filter!.Render(new RenderArgs<AcceptedInvitation>(BsonSerializer.SerializerRegistry.GetSerializer<AcceptedInvitation>(), BsonSerializer.SerializerRegistry));
        Assert.Contains("shared-subject", rendered.ToString());
    }
}
#endif
