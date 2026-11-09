// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Ante.Invitations.Accepting;

/// <summary>
/// Strict claim readers shared by Ante's signed-assertion verifiers. Every reader refuses an ambiguous value
/// (repeated, blank, padded or oversized) instead of picking one, so a verifier can fail closed.
/// </summary>
internal static class StrictJwtClaims
{
    /// <summary>Reads a claim that must appear exactly once, untrimmed and at most 2048 characters long.</summary>
    /// <param name="jwt">The validated token.</param>
    /// <param name="name">The claim name.</param>
    /// <returns>The single value, or null when absent or ambiguous.</returns>
    internal static string? ExactlyOne(JsonWebToken jwt, string name)
    {
        var values = jwt.Claims.Where(claim => claim.Type == name).Select(claim => claim.Value).ToArray();
        return values.Length == 1 && values[0].Length <= 2048 &&
            !string.IsNullOrWhiteSpace(values[0]) && values[0] == values[0].Trim()
            ? values[0]
            : null;
    }

    /// <summary>Reads a single NumericDate claim in whole seconds.</summary>
    /// <param name="jwt">The validated token.</param>
    /// <param name="name">The claim name.</param>
    /// <returns>The instant, or null when absent, ambiguous or out of range.</returns>
    internal static DateTimeOffset? NumericDate(JsonWebToken jwt, string name)
    {
        var raw = ExactlyOne(jwt, name);
        return long.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) &&
            seconds is >= -62135596800 and <= 253402300799
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
    }

    /// <summary>Whether a value is the canonical unpadded base64url encoding of exactly 32 bytes.</summary>
    /// <param name="value">The value to check.</param>
    /// <returns>Whether the value is canonical.</returns>
    internal static bool IsCanonicalDigest(string? value)
    {
        if (value is not { Length: 43 })
        {
            return false;
        }

        try
        {
            var decoded = WebEncoders.Base64UrlDecode(value);
            return decoded.Length == SHA256.HashSizeInBytes && WebEncoders.Base64UrlEncode(decoded) == value;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
