// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;

namespace Ante.Invitations.Issuing.for_InvitationTokenConfigurationValidator.when_validating_trust_settings.given;

public class a_production_token_configuration : Specification
{
    protected InvitationTokenConfig _config = null!;

    void Establish()
    {
        using var rsa = RSA.Create(2048);
        _config = new()
        {
            PrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem(),
            Issuer = "ante",
            Audience = "ante-lobby",
        };
    }
}
#endif
