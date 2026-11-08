// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.for_BackchannelCredentials.given;
using Ante.Invitations.UserSetup;
using Microsoft.Extensions.Options;

namespace Ante.for_BackchannelCredentials.when_sending;

public class the_identity_backchannel_has_a_credential : Specification
{
    RecordingHandler _handler = null!;
    RecordingLogger<IdentityBackchannel> _logger = null!;

    void Establish()
    {
        _handler = new("{\"isInUse\":true}");
        _logger = new();
    }

    async Task Because() => await new IdentityBackchannel(
        new HttpClient(_handler),
        Options.Create(new AnteOptions { IdentityBackchannelUrl = "https://host.example.com/api", IdentityBackchannelCredential = Credentials.Valid }),
        _logger).IsSubjectAlreadyAssociatedWithAUser(new("acme"), "subject-1");

    [Fact] void should_send_the_bearer_credential() => Assert.Equal($"Bearer {Credentials.Valid}", _handler.Requests[0].Headers.Authorization?.ToString());
    [Fact] void should_not_put_the_credential_in_the_url() => Assert.DoesNotContain(Credentials.Valid, _handler.Requests[0].RequestUri!.ToString());
}
#endif
