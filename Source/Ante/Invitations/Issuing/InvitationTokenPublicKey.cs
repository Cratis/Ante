// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;

namespace Ante.Invitations.Issuing;

// Serialize only this allow-list, never a signing key or a general-purpose JWK object.
internal sealed record InvitationTokenPublicKey(
    [property: JsonPropertyName("kty")] string KeyType,
    [property: JsonPropertyName("use")] string Use,
    [property: JsonPropertyName("alg")] string Algorithm,
    [property: JsonPropertyName("kid")] string KeyId,
    [property: JsonPropertyName("n")] string Modulus,
    [property: JsonPropertyName("e")] string Exponent)
{
    public static InvitationTokenPublicKey From(RsaSecurityKey key) => new(
        "RSA",
        "sig",
        "RS256",
        key.KeyId,
        Base64UrlEncoder.Encode(key.Parameters.Modulus),
        Base64UrlEncoder.Encode(key.Parameters.Exponent));
}
