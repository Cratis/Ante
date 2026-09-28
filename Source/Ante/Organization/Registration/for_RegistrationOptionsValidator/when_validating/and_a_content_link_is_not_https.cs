// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Organization.Registration.for_RegistrationOptionsValidator.when_validating;

public class and_a_content_link_is_not_https : Specification
{
    Exception? _error;

    void Because() => _error = Cratis.Specifications.Catch.Exception(() => RegistrationOptionsValidator.Validate(new AnteOptions
    {
        Registration = new()
        {
            Content = new Dictionary<string, RegistrationContent> { ["en"] = new() { PricingUrl = "javascript:alert(1)" } },
        },
    }));

    [Fact] void should_fail_startup() => Assert.IsType<RegistrationConfigurationInvalid>(_error);
}
#endif
