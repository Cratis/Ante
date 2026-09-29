// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.IdentityProviders;
using Ante.Integration.given;
using Ante.Invitations.Accepting;
using Ante.Invitations.Issuing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Ante.Integration.Invitations.when_exchanging_while_indexes_are_pending;

[Collection(ChronicleCollection.Name)]
public class and_the_request_uses_the_controller : a_running_ante
{
    readonly Guid _id = NewInvitationId();
    int _status;
    long _sessions;

    protected override IExchangeIndexReadiness? ExchangeIndexes => new PendingExchangeIndexReadiness();

    async Task Because()
    {
        var token = await Invite(Host, _id, JoinInvitation());
        await using var scope = Ante.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var controller = new InviteExchangeController(
            services.GetRequiredService<IMongoCollection<AcceptedInvitation>>(),
            services.GetRequiredService<IIdentityProviderResolver>(),
            services.GetRequiredService<IInvitationTokenValidator>(),
            services.GetRequiredService<ILogger<InviteExchangeBypassMiddleware>>(),
            services.GetRequiredService<IExchangeIndexReadiness>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        controller.Request.Headers.Authorization = $"Bearer {token.Token}";
        var result = await controller.Exchange(new ExchangeInviteRequest($"subject-{Guid.NewGuid():N}", AnteApplication.IdentityProvider, null, null));
        _status = ((StatusCodeResult)result).StatusCode;
        _sessions = await services.GetRequiredService<IMongoCollection<AcceptedInvitation>>()
            .CountDocumentsAsync(FilterDefinition<AcceptedInvitation>.Empty);
    }

    [Fact] void should_respond_service_unavailable() => _status.ShouldEqual(StatusCodes.Status503ServiceUnavailable);
    [Fact] void should_not_write_a_session_to_writable_mongo() => _sessions.ShouldEqual(0);
}
