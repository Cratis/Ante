// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Ante.Invitations.Accepting.for_AttestedInvitationCompletion.given;
using Ante.Invitations.Issuing;
using Microsoft.AspNetCore.Http;

namespace Ante.Invitations.Accepting.for_InviteExchangeBypassMiddleware.given;

public class an_attested_completion_request : a_staged_completion
{
    protected string Body = string.Empty;
    protected int Status;
    protected IInvitationTokenValidator LegacyValidator = null!;
    protected bool IndexesReady = true;
    protected bool NextCalled;

    void Establish()
    {
        Body = $"{{\"invitationTransaction\":\"{Stage.Transaction}\"}}";
        LegacyValidator = Substitute.For<IInvitationTokenValidator>();
    }

    protected async Task Post(string path = "/_invite/exchange")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = path;
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(Body));
        context.Request.Headers.Authorization = $"Bearer {Sign()}";
        var middleware = new AttestedInviteExchangeMiddleware(_ =>
        {
            NextCalled = true;
            return Task.CompletedTask;
        });
        var staging = new AttestedInvitationStaging(_verifier, LegacyValidator, Substitute.For<IEventStore>(), Transactions);
        var indexes = Substitute.For<IExchangeIndexReadiness>();
        indexes.IsReady.Returns(IndexesReady);
        await middleware.InvokeAsync(context, staging, Completion, indexes);
        Status = context.Response.StatusCode;
    }
}
#endif
