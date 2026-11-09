// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;

namespace Ante.Onboarding.Provisioning.for_HostOutcomesConfigurationValidator.when_validating;

public class and_durable_outcomes_are_misconfigured : given.a_durable_configuration
{
    readonly List<string> _started = [];

    void Because()
    {
        using var shortKey = RSA.Create(1024);
        var cases = new (string Name, HostOutcomesConfig Config)[]
        {
            ("no pinned keys", Configure(_ => _.Keys = [])),
            ("a blank audience", Configure(_ => _.Audience = " ")),
            ("a lifetime that is too short", Configure(_ => _.MaximumLifetimeSeconds = 5)),
            ("a lifetime that is too long", Configure(_ => _.MaximumLifetimeSeconds = 3600)),
            ("a key for a host that is not configured", Configuration(Key(host: "Unknown"))),
            ("a duplicate key id", Configuration(Key(), Key())),
            ("a private key", Configuration(Key(pem: _key.ExportPkcs8PrivateKeyPem()))),
            ("a key that is not a key", Configuration(Key(pem: "not a key"))),
            ("a key shorter than 2048 bits", Configuration(Key(pem: shortKey.ExportSubjectPublicKeyInfoPem()))),
            ("an undefined mode", Configure(_ => _.Mode = (HostOutcomesMode)42)),
        };

        foreach (var (name, config) in cases)
        {
            if (Cratis.Specifications.Catch.Exception(() => HostOutcomesConfigurationValidator.Validate(config, _hostStores)) is not HostOutcomesMisconfigured)
            {
                _started.Add(name);
            }
        }
    }

    [Fact] void should_refuse_to_start_for_every_case() => _started.ShouldBeEmpty();
}
#endif
