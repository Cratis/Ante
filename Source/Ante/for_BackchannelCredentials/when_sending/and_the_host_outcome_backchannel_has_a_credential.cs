// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.for_BackchannelCredentials.given;
using Ante.Invitations;
using Ante.Invitations.HostOutcome;
using Microsoft.Extensions.Options;

namespace Ante.for_BackchannelCredentials.when_sending;

public class and_the_host_outcome_backchannel_has_a_credential : Specification
{
    RecordingHandler _handler = null!;

    void Establish() => _handler = new("{\"status\":\"pending\"}");

    async Task Because() => await new HostOutcomeBackchannel(
        new HttpClient(_handler),
        Options.Create(new AnteOptions { HostOutcomeUrl = "https://host.example.com/api", HostOutcomeCredential = Credentials.Valid }),
        new RecordingLogger<HostOutcomeBackchannel>()).GetOutcome(InvitationId.New());

    [Fact] void should_send_the_bearer_credential() => Assert.Equal($"Bearer {Credentials.Valid}", _handler.Requests[0].Headers.Authorization?.ToString());
    [Fact] void should_not_put_the_credential_in_the_url() => Assert.DoesNotContain(Credentials.Valid, _handler.Requests[0].RequestUri!.ToString());
}
#endif
