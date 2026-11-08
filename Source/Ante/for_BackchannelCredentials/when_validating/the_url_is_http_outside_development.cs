// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.for_BackchannelCredentials.given;

namespace Ante.for_BackchannelCredentials.when_validating;

public class the_url_is_http_outside_development : Specification
{
    Exception? _identity;
    Exception? _outcome;

    void Because()
    {
        _identity = Cratis.Specifications.Catch.Exception(() => BackchannelCredentials.Validate(
            new AnteOptions { IdentityBackchannelUrl = "http://h.example.com", IdentityBackchannelCredential = Credentials.Valid }, false));
        _outcome = Cratis.Specifications.Catch.Exception(() => BackchannelCredentials.Validate(
            new AnteOptions { HostOutcomeUrl = "http://h.example.com", HostOutcomeCredential = Credentials.Valid }, false));
    }

    [Fact] void should_reject_the_identity_backchannel() => Assert.IsType<BackchannelCredentialInvalid>(_identity);
    [Fact] void should_reject_the_host_outcome_backchannel() => Assert.IsType<BackchannelCredentialInvalid>(_outcome);
}
#endif
