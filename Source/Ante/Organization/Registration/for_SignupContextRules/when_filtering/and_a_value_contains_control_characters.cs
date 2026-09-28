// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Organization.Registration.for_SignupContextRules.when_filtering;

public class and_a_value_contains_control_characters : Specification
{
    IReadOnlyList<Ante.Contracts.Organization.SignupContextEntry> _result = null!;

    void Because() => _result = SignupContextRules.Filter(
        new RegistrationOptions { ContextKeys = ["offer"] },
        [new("offer", "trial\nsecond-line")]);

    [Fact] void should_drop_it() => Assert.Empty(_result);
}
#endif
