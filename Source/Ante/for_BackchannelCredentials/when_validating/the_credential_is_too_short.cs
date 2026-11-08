// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.for_BackchannelCredentials.given;

namespace Ante.for_BackchannelCredentials.when_validating;

public class the_credential_is_too_short : Specification
{
    Exception? _identity;
    Exception? _outcome;

    void Because()
    {
        _identity = Cratis.Specifications.Catch.Exception(() => BackchannelCredentials.Validate(
            new AnteOptions { IdentityBackchannelUrl = "https://h.example.com", IdentityBackchannelCredential = Credentials.TooShort }, false));
        _outcome = Cratis.Specifications.Catch.Exception(() => BackchannelCredentials.Validate(
            new AnteOptions { HostOutcomeUrl = "https://h.example.com", HostOutcomeCredential = Credentials.TooShort }, true));
    }

    [Fact] void should_reject_the_identity_credential() => Assert.IsType<BackchannelCredentialInvalid>(_identity);
    [Fact] void should_reject_the_outcome_credential_even_in_development() => Assert.IsType<BackchannelCredentialInvalid>(_outcome);
    [Fact] void should_not_reveal_the_credential() => Assert.DoesNotContain(Credentials.TooShort, _identity!.Message);
}
#endif
