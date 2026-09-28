// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Ante.IdentityProviders;
using Ante.Invitations.Accepting.for_AttestedInvitationCompletion.given;
using Ante.Invitations.Issuing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_InviteExchangeBypassMiddleware.given;

public class an_attested_completion_request : a_staged_completion
{
    protected string Body = string.Empty;
    protected int Status;
    protected IInvitationTokenValidator LegacyValidator = null!;

    void Establish()
    {
        Body = $"{{\"invitationTransaction\":\"{Stage.Transaction}\"}}";
        LegacyValidator = Substitute.For<IInvitationTokenValidator>();
    }

    protected async Task Post()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/_invite/exchange";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(Body));
        context.Request.Headers.Authorization = $"Bearer {Sign()}";
        var middleware = new InviteExchangeBypassMiddleware(_ => Task.CompletedTask);
        var staging = new AttestedInvitationStaging(_verifier, LegacyValidator, Substitute.For<IEventStore>(), Transactions);
        await middleware.InvokeAsync(
            context,
            Substitute.For<IMongoCollection<AcceptedInvitation>>(),
            Substitute.For<IIdentityProviderResolver>(),
            LegacyValidator,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<InviteExchangeBypassMiddleware>.Instance,
            Options.Create(_configuration),
            staging,
            Completion);
        Status = context.Response.StatusCode;
    }
}
#endif
