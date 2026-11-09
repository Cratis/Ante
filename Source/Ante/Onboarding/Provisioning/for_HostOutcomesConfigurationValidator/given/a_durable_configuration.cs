// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;

namespace Ante.Onboarding.Provisioning.for_HostOutcomesConfigurationValidator.given;

public abstract class a_durable_configuration : Specification
{
    protected static readonly string[] _hostStores = ["Host"];
    protected static readonly RSA _key = RSA.Create(2048);

    protected static HostOutcomeSigningKey Key(string keyId = "host-key", string host = "Host", string? pem = null) =>
        new() { KeyId = keyId, Host = host, PublicKeyPem = pem ?? _key.ExportSubjectPublicKeyInfoPem() };

    protected static HostOutcomesConfig Configuration(params HostOutcomeSigningKey[] keys) => new()
    {
        Mode = HostOutcomesMode.DurableRequired,
        Audience = "ante-lobby",
        MaximumLifetimeSeconds = 60,
        Keys = keys.Length == 0 ? [Key()] : [.. keys],
    };

    protected static HostOutcomesConfig Configure(Action<HostOutcomesConfig> change)
    {
        var config = Configuration();
        change(config);
        return config;
    }
}
#endif
