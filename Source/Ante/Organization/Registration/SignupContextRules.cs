// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Organization;

namespace Ante.Organization.Registration;

/// <summary>
/// Filters host-supplied signup context to what the deployment allows. Context is never trusted: it is
/// whatever was on the link the visitor followed, so it is bounded, allowlisted and passed through as
/// plain text for the host to validate.
/// </summary>
public static class SignupContextRules
{
    /// <summary>
    /// Determines whether a key is well formed.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True when the key is non-empty, bounded and uses only letters, digits, '_', '-' or '.'.</returns>
    public static bool IsValidKey(string? key) =>
        !string.IsNullOrEmpty(key) &&
        key.Length <= RegistrationOptions.MaximumContextLength &&
        key.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.');

    /// <summary>
    /// Keeps only allowed keys with bounded, control-character-free values.
    /// </summary>
    /// <param name="options">The registration options holding the allowlist.</param>
    /// <param name="context">The context the client submitted, if any.</param>
    /// <returns>The filtered context, ordered by key; for a repeated key the first entry wins.</returns>
    public static IReadOnlyList<SignupContextEntry> Filter(RegistrationOptions options, IEnumerable<SignupContextEntry>? context)
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in context ?? [])
        {
            if (key is null || result.ContainsKey(key) || !options.ContextKeys.Contains(key, StringComparer.Ordinal) || value is null)
            {
                continue;
            }

            var trimmed = value.Trim();
            if (trimmed.Length == 0 || trimmed.Length > RegistrationOptions.MaximumContextLength || trimmed.Any(char.IsControl))
            {
                continue;
            }

            result[key] = trimmed;
        }

        return [.. result.Select(entry => new SignupContextEntry(entry.Key, entry.Value))];
    }
}
