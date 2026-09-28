// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.IdentityProviders;
using Ante.Invitations.Issuing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_InviteExchangeController.when_exchanging;

public class and_attested_mode_receives_a_legacy_body : Specification
{
    IActionResult _result = null!;
    IInvitationTokenValidator _tokenValidator = null!;

    void Establish() => _tokenValidator = Substitute.For<IInvitationTokenValidator>();

    async Task Because()
    {
        var controller = new InviteExchangeController(
            Substitute.For<IMongoCollection<AcceptedInvitation>>(),
            Substitute.For<IIdentityProviderResolver>(),
            _tokenValidator,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<InviteExchangeBypassMiddleware>.Instance,
            Options.Create(new InvitationExchangeConfig { Mode = InvitationExchangeMode.Attested }));
        _result = await controller.Exchange(new ExchangeInviteRequest("subject", "provider", null, null));
    }

    [Fact] void should_refuse_the_unsigned_exchange() => Assert.IsType<BadRequestResult>(_result);
    [Fact] async Task should_never_consult_the_legacy_token_validator() => await _tokenValidator.DidNotReceiveWithAnyArgs().Validate(default!);
}
#endif
