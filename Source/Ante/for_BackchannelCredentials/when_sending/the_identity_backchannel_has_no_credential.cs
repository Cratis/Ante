// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.for_BackchannelCredentials.given;
using Ante.Invitations.UserSetup;
using Microsoft.Extensions.Options;

namespace Ante.for_BackchannelCredentials.when_sending;

public class the_identity_backchannel_has_no_credential : Specification
{
    RecordingHandler _handler = null!;

    void Establish() => _handler = new("{\"isInUse\":false}");

    async Task Because() => await new IdentityBackchannel(
        new HttpClient(_handler),
        Options.Create(new AnteOptions { IdentityBackchannelUrl = "https://host.example.com/api" }),
        new RecordingLogger<IdentityBackchannel>()).IsSubjectAlreadyAssociatedWithAUser(new("acme"), "subject-1");

    [Fact] void should_call_the_host() => Assert.Single(_handler.Requests);
    [Fact] void should_send_no_authorization() => Assert.Null(_handler.Requests[0].Headers.Authorization);
}
#endif
