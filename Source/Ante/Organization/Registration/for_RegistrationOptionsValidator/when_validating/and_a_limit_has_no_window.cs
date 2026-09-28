// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Organization.Registration.for_RegistrationOptionsValidator.when_validating;

public class and_a_limit_has_no_window : Specification
{
    Exception? _error;

    void Because() => _error = Cratis.Specifications.Catch.Exception(() => RegistrationOptionsValidator.Validate(new AnteOptions
    {
        Registration = new() { MaxPerIdentity = 2, Window = TimeSpan.Zero },
    }));

    [Fact] void should_fail_startup() => Assert.IsType<RegistrationConfigurationInvalid>(_error);
}
#endif
