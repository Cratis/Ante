// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.for_BackchannelCredentials.given;
using Ante.Invitations;
using Ante.Invitations.HostOutcome;
using Microsoft.Extensions.Options;

namespace Ante.for_BackchannelCredentials.when_sending;

public class the_host_outcome_backchannel_has_no_credential : Specification
{
    RecordingHandler _handler = null!;

    void Establish() => _handler = new("{\"status\":\"pending\"}");

    async Task Because() => await new HostOutcomeBackchannel(
        new HttpClient(_handler),
        Options.Create(new AnteOptions { HostOutcomeUrl = "https://host.example.com/api" }),
        new RecordingLogger<HostOutcomeBackchannel>()).GetOutcome(InvitationId.New());

    [Fact] void should_send_no_authorization() => Assert.Null(_handler.Requests[0].Headers.Authorization);
}
#endif
