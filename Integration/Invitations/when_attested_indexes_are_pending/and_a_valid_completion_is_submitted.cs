// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Ante.Integration.given;
using Ante.Invitations.Accepting;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ante.Integration.Invitations.when_attested_indexes_are_pending;

[Collection(ChronicleCollection.Name)]
public class and_a_valid_completion_is_submitted : a_running_ante
{
    readonly Guid _id = NewInvitationId();
    readonly MutableExchangeIndexReadiness _indexes = new();
    HttpStatusCode _response;
    long _stages;
    long _sessions;

    protected override bool AttestedExchange => true;
    protected override IExchangeIndexReadiness? ExchangeIndexes => _indexes;

    async Task Because()
    {
        var invitation = JoinInvitation();
        var issued = await Invite(Host, _id, invitation);
        var requests = new AttestedExchangeRequests(Ante, _id, issued.Token, invitation.Email.Value, $"actor-{Guid.NewGuid():N}");
        using var client = Ante.CreateClient();
        using (var stageRequest = requests.Stage())
        using (var stageResponse = await client.SendAsync(stageRequest))
        {
            stageResponse.EnsureSuccessStatusCode();
        }

        _indexes.IsReady = false;
        using var completionRequest = requests.Complete();
        using var response = await client.SendAsync(completionRequest);
        _response = response.StatusCode;

        await using var scope = Ante.Services.CreateAsyncScope();
        _stages = await scope.ServiceProvider.GetRequiredService<IMongoCollection<StagedInvitationTransaction>>()
            .CountDocumentsAsync(FilterDefinition<StagedInvitationTransaction>.Empty);
        _sessions = await scope.ServiceProvider.GetRequiredService<IMongoCollection<AttestedInvitationSession>>()
            .CountDocumentsAsync(FilterDefinition<AttestedInvitationSession>.Empty);
    }

    [Fact] void should_refuse_the_completion_until_all_indexes_exist() => _response.ShouldEqual(HttpStatusCode.ServiceUnavailable);
    [Fact] void should_keep_the_completed_stage() => _stages.ShouldEqual(1);
    [Fact] void should_not_write_a_session_to_writable_mongo() => _sessions.ShouldEqual(0);
}
