// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.for_BackchannelCredentials.given;

namespace Ante.for_BackchannelCredentials.when_validating;

public class and_the_url_is_https_with_a_credential : Specification
{
    Exception? _error;

    void Because() => _error = Cratis.Specifications.Catch.Exception(() => BackchannelCredentials.Validate(
        new AnteOptions { IdentityBackchannelUrl = "https://h.example.com", IdentityBackchannelCredential = Credentials.Valid }, false));

    [Fact] void should_accept_it() => Assert.Null(_error);
}
#endif
