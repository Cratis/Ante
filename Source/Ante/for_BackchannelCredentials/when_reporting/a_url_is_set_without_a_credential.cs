// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.for_BackchannelCredentials.given;

namespace Ante.for_BackchannelCredentials.when_reporting;

public class a_url_is_set_without_a_credential : Specification
{
    RecordingLogger<AnteOptions> _production = null!;
    RecordingLogger<AnteOptions> _development = null!;
    RecordingLogger<AnteOptions> _credentialed = null!;

    void Because()
    {
        var options = new AnteOptions { IdentityBackchannelUrl = "https://h.example.com/secret-path", HostOutcomeUrl = "https://h.example.com" };
        _production = new();
        _development = new();
        _credentialed = new();
        BackchannelCredentials.Report(options, false, _production);
        BackchannelCredentials.Report(options, true, _development);
        BackchannelCredentials.Report(
            new AnteOptions
            {
                IdentityBackchannelUrl = options.IdentityBackchannelUrl,
                IdentityBackchannelCredential = Credentials.Valid,
                HostOutcomeUrl = options.HostOutcomeUrl,
                HostOutcomeCredential = Credentials.Valid,
            },
            false,
            _credentialed);
    }

    [Fact] void should_warn_for_each_backchannel_outside_development() => Assert.Equal(2, _production.Messages.Count(m => m.StartsWith("Warning")));
    [Fact] void should_not_log_the_url() => Assert.DoesNotContain(_production.Messages, m => m.Contains("secret-path"));
    [Fact] void should_not_warn_in_development() => Assert.Empty(_development.Messages);
    [Fact] void should_not_warn_when_credentials_are_configured() => Assert.Empty(_credentialed.Messages);
}
#endif
