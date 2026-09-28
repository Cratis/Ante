// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Organization.Registration.for_RegistrationOptionsValidator.when_validating;

public class and_options_are_left_at_their_defaults : Specification
{
    Exception? _error;

    void Because() => _error = Cratis.Specifications.Catch.Exception(() => RegistrationOptionsValidator.Validate(new AnteOptions()));

    [Fact] void should_accept_them() => Assert.Null(_error);
    [Fact] void should_offer_registration_by_default() => Assert.True(new AnteOptions().Registration.Enabled);
}
#endif
