// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;
using Ante.Invitations.Issuing;

namespace Ante.Invitations.Accepting.for_InvitationExchangeConfigurationValidator.when_validating_configuration;

public class and_attested_trust_is_complete : Specification
{
    Exception? _error;

    void Because()
    {
        using var rsa = RSA.Create(2048);
        var exchange = new InvitationExchangeConfig
        {
            Mode = InvitationExchangeMode.Attested,
            Attestation = new InvitationAttestationTrustConfig
            {
                Issuer = "https://auth.example.com",
                Audience = "ante-lobby",
                LobbyScope = "lobby",
                PublicKeys = [new InvitationAttestationPublicKey { KeyId = "proxy-key", PublicKeyPem = rsa.ExportSubjectPublicKeyInfoPem() }],
                Providers = [new InvitationAttestationProvider { Key = "oidc", Issuer = "https://id.example.com", AcceptableAssurances = ["mfa"] }],
            },
        };
        var capability = new InvitationTokenConfig { Issuer = "https://ante.example.com", Audience = "proxy", PrivateKeyPem = rsa.ExportRSAPrivateKeyPem() };
        _error = Record.Exception(() => InvitationExchangeConfigurationValidator.Validate(exchange, capability));
    }

    [Fact] void should_allow_startup() => Assert.Null(_error);
}
#endif
