// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.for_BackchannelCredentials.given;
using Ante.Invitations.UserSetup;
using Microsoft.Extensions.Options;

namespace Ante.for_BackchannelCredentials.when_sending;

public class and_the_host_is_unreachable : Specification
{
    sealed class Failing : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("connection refused");
    }

    RecordingLogger<IdentityBackchannel> _logger = null!;
    bool _result;

    void Establish() => _logger = new();

    async Task Because() => _result = await new IdentityBackchannel(
        new HttpClient(new Failing()),
        Options.Create(new AnteOptions { IdentityBackchannelUrl = "https://host.example.com/api", IdentityBackchannelCredential = Credentials.Valid }),
        _logger).IsSubjectAlreadyAssociatedWithAUser(new("acme"), "subject-1");

    [Fact] void should_still_fail_open() => Assert.False(_result);
    [Fact] void should_log_the_outage() => Assert.Single(_logger.Messages);
    [Fact] void should_never_log_the_credential() => Assert.DoesNotContain(_logger.Messages, message => message.Contains(Credentials.Valid));
}
#endif
