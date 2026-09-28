// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;

namespace Ante.Organization.Registration.for_SignupContextRules.when_filtering;

public class and_the_context_mixes_allowed_and_unknown_keys : Specification
{
    IReadOnlyDictionary<string, string> _result = null!;

    void Because() => _result = SignupContextRules.Filter(
        new RegistrationOptions { ContextKeys = ["offer", "utm_source", "utm_campaign"] },
        [
            new("offer", "  trial "),
            new("offer", "second"),
            new("utm_source", "cratis.studio"),
            new("utm_campaign", new string('x', RegistrationOptions.MaximumContextLength + 1)),
            new("plan", "enterprise"),
            new("Offer", "sneaky"),
        ]).ToDictionary(entry => entry.Key, entry => entry.Value);

    [Fact] void should_keep_allowed_keys_trimmed() => Assert.Equal("trial", _result["offer"]);
    [Fact] void should_keep_other_allowed_keys() => Assert.Equal("cratis.studio", _result["utm_source"]);
    [Fact] void should_drop_values_that_are_too_long() => Assert.False(_result.ContainsKey("utm_campaign"));
    [Fact] void should_drop_keys_that_are_not_allowed() => Assert.False(_result.ContainsKey("plan"));
    [Fact] void should_match_keys_exactly() => Assert.False(_result.ContainsKey("Offer"));
}
#endif
