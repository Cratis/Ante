// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Ante.Integration.given;
using Ante.Invitations.Accepting;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ante.Integration.Invitations.when_attested_indexes_are_pending;

[Collection(ChronicleCollection.Name)]
public class and_a_valid_stage_is_submitted : a_running_ante
{
    readonly Guid _id = NewInvitationId();
    readonly MutableExchangeIndexReadiness _indexes = new();
    HttpStatusCode _response;
    long _stages;

    protected override bool AttestedExchange => true;
    protected override IExchangeIndexReadiness? ExchangeIndexes => _indexes;

    async Task Because()
    {
        var invitation = JoinInvitation();
        var issued = await Invite(Host, _id, invitation);
        var requests = new AttestedExchangeRequests(Ante, _id, issued.Token, invitation.Email.Value, $"actor-{Guid.NewGuid():N}");
        _indexes.IsReady = false;
        using var client = Ante.CreateClient();
        using var request = requests.Stage();
        using var response = await client.SendAsync(request);
        _response = response.StatusCode;

        await using var scope = Ante.Services.CreateAsyncScope();
        _stages = await scope.ServiceProvider.GetRequiredService<IMongoCollection<StagedInvitationTransaction>>()
            .CountDocumentsAsync(FilterDefinition<StagedInvitationTransaction>.Empty);
    }

    [Fact] void should_refuse_the_stage_until_all_indexes_exist() => _response.ShouldEqual(HttpStatusCode.ServiceUnavailable);
    [Fact] void should_not_write_a_transaction_to_writable_mongo() => _stages.ShouldEqual(0);
}
