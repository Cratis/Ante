// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.IdentityProviders;
using Ante.Invitations.Issuing;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_InviteExchangeController.when_exchanging;

public class and_request_body_is_missing : Specification
{
    IActionResult _result = null!;

    async Task Because()
    {
        var controller = new InviteExchangeController(
            Substitute.For<IMongoCollection<AcceptedInvitation>>(),
            Substitute.For<IIdentityProviderResolver>(),
            Substitute.For<IInvitationTokenValidator>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<InviteExchangeBypassMiddleware>.Instance,
            Substitute.For<IExchangeIndexReadiness>());
        _result = await controller.Exchange(null);
    }

    [Fact] void should_return_bad_request() => Assert.IsType<BadRequestResult>(_result);
}
#endif
