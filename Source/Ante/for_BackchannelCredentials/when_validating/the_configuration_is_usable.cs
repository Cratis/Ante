// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.for_BackchannelCredentials.given;

namespace Ante.for_BackchannelCredentials.when_validating;

public class the_configuration_is_usable : Specification
{
    Exception? _https;
    Exception? _development;
    Exception? _unset;

    void Because()
    {
        _https = Cratis.Specifications.Catch.Exception(() => BackchannelCredentials.Validate(
            new AnteOptions { IdentityBackchannelUrl = "https://h.example.com", IdentityBackchannelCredential = Credentials.Valid }, false));
        _development = Cratis.Specifications.Catch.Exception(() => BackchannelCredentials.Validate(
            new AnteOptions { HostOutcomeUrl = "http://localhost:5000", HostOutcomeCredential = Credentials.Valid }, true));
        _unset = Cratis.Specifications.Catch.Exception(() => BackchannelCredentials.Validate(
            new AnteOptions { IdentityBackchannelUrl = "http://h.internal", HostOutcomeUrl = "http://h.internal" }, false));
    }

    [Fact] void should_accept_https_with_a_credential() => Assert.Null(_https);
    [Fact] void should_accept_http_in_development() => Assert.Null(_development);
    [Fact] void should_leave_credential_less_deployments_unchanged() => Assert.Null(_unset);
}
#endif
