// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Ante.Invitations.Issuing;

internal sealed record InvitationTokenMetadata(
    [property: JsonPropertyName("issuer")] string Issuer,
    [property: JsonPropertyName("audience")] string Audience,
    [property: JsonPropertyName("jwks_uri")] string JwksUri,
    [property: JsonPropertyName("token_signing_alg_values_supported")] IReadOnlyList<string> Algorithms);
