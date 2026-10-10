// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_InviteExchangeProcessor.when_exchanging;

// One person routinely holds several invitations to the same organization - a resend, a refresh and a revocation
// each mint a new id. Opening a second link must not replace the session of the first (Cratis/StudioIssues#581, #582).
public class and_the_login_exchanges_a_second_invitation : Specification
{
    readonly InvitationId _first = Guid.NewGuid();
    readonly InvitationId _second = Guid.NewGuid();
    BsonDocument _firstFilter;
    BsonDocument _secondFilter;

    void Because()
    {
        InvitationMongoSerialization.EnsureConfigured();
        _firstFilter = Render(InviteExchangeProcessor.SessionFilter("subject-123", "github", _first));
        _secondFilter = Render(InviteExchangeProcessor.SessionFilter("subject-123", "github", _second));
    }

    static BsonDocument Render(FilterDefinition<AcceptedInvitation> filter) =>
        filter.Render(new RenderArgs<AcceptedInvitation>(BsonSerializer.SerializerRegistry.GetSerializer<AcceptedInvitation>(), BsonSerializer.SerializerRegistry));

    [Fact] void should_select_the_session_by_invitation() => _firstFilter["invitationId"].AsGuid.ShouldEqual(_first.Value);
    [Fact] void should_not_select_the_other_invitations_session() => _firstFilter["invitationId"].AsGuid.ShouldNotEqual(_second.Value);
    [Fact] void should_select_a_different_session_per_invitation() => _firstFilter.ShouldNotEqual(_secondFilter);
    [Fact] void should_still_select_by_login() => _firstFilter["Subject"].AsString.ShouldEqual("subject-123");
}
#endif
