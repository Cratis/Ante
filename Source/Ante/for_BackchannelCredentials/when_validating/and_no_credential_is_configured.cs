// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.for_BackchannelCredentials.when_validating;

public class and_no_credential_is_configured : Specification
{
    Exception? _error;

    void Because() => _error = Cratis.Specifications.Catch.Exception(() => BackchannelCredentials.Validate(
        new AnteOptions { IdentityBackchannelUrl = "http://h.internal", HostOutcomeUrl = "http://h.internal" }, false));

    [Fact] void should_leave_the_deployment_unchanged() => Assert.Null(_error);
}
#endif
