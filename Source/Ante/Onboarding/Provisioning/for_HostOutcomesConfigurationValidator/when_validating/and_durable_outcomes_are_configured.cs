// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Onboarding.Provisioning.for_HostOutcomesConfigurationValidator.when_validating;

public class and_durable_outcomes_are_configured : given.a_durable_configuration
{
    Exception? _exception;

    void Because() => _exception = Cratis.Specifications.Catch.Exception(() => HostOutcomesConfigurationValidator.Validate(Configuration(), _hostStores));

    [Fact] void should_start() => _exception.ShouldBeNull();
}
#endif
