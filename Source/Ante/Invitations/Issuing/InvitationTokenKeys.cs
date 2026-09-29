// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Ante.Invitations.Issuing;

/// <summary>
/// Names invitation signing keys by a stable key id, so a verifier can tell which trusted key signed a token.
/// </summary>
public static class InvitationTokenKeys
{
    /// <summary>
    /// Gets the key id of an RSA key: its RFC 7638 JWK thumbprint, base64url encoded.
    /// </summary>
    /// <param name="rsa">The key, public or private.</param>
    /// <returns>The key id.</returns>
    public static string KeyIdFor(RSA rsa)
    {
        var parameters = rsa.ExportParameters(false);
        var jwk = new JsonWebKey
        {
            Kty = JsonWebAlgorithmsKeyTypes.RSA,
            E = Base64UrlEncoder.Encode(parameters.Exponent),
            N = Base64UrlEncoder.Encode(parameters.Modulus),
        };
        return Base64UrlEncoder.Encode(jwk.ComputeJwkThumbprint());
    }

    /// <summary>
    /// Gets the trusted public keys for exchange: the one derived from the signing key, the configured public key,
    /// and every previous key, each named by its key id.
    /// </summary>
    /// <param name="config">The token configuration.</param>
    /// <returns>The trusted keys.</returns>
    public static IReadOnlyList<SecurityKey> TrustedKeys(InvitationTokenConfig config)
    {
        var pems = new[] { config.PrivateKeyPem, config.PublicKeyPem }
            .Concat(config.PreviousPublicKeyPems ?? [])
            .Where(pem => !string.IsNullOrWhiteSpace(pem));
        var keys = new Dictionary<string, SecurityKey>(StringComparer.Ordinal);
        foreach (var pem in pems)
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            var keyId = KeyIdFor(rsa);
            keys.TryAdd(keyId, new RsaSecurityKey(rsa.ExportParameters(false)) { KeyId = keyId });
        }

        return [.. keys.Values];
    }
}
