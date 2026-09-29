// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Ante.Integration.given;
using Ante.Invitations.Accepting;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ante.Integration.Invitations.when_exchanging_while_indexes_are_pending;

[Collection(ChronicleCollection.Name)]
public class and_the_request_uses_the_middleware : a_running_ante
{
    readonly Guid _id = NewInvitationId();
    HttpStatusCode _status;
    long _sessions;

    protected override IExchangeIndexReadiness? ExchangeIndexes => new PendingExchangeIndexReadiness();

    async Task Because()
    {
        var token = await Invite(Host, _id, JoinInvitation());
        using var response = await Ante.ExchangeInvitation(token.Token, $"subject-{Guid.NewGuid():N}");
        _status = response.StatusCode;
        await using var scope = Ante.Services.CreateAsyncScope();
        _sessions = await scope.ServiceProvider.GetRequiredService<IMongoCollection<AcceptedInvitation>>()
            .CountDocumentsAsync(FilterDefinition<AcceptedInvitation>.Empty);
    }

    [Fact] void should_respond_service_unavailable() => _status.ShouldEqual(HttpStatusCode.ServiceUnavailable);
    [Fact] void should_not_write_a_session_to_writable_mongo() => _sessions.ShouldEqual(0);
}
