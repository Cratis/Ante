// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Ante.Configuration;

namespace Ante.Invitations;

/// <summary>Resolves one invitation journey's copy without consulting registration settings.</summary>
public static class InvitationContentResolver
{
    /// <summary>Resolves exact locale, neutral language, en, then en-US; absent fields retain frontend defaults.</summary>
    /// <param name="options">The journey's configured content.</param>
    /// <param name="culture">The negotiated request culture.</param>
    /// <returns>Only allowlisted, public presentation fields.</returns>
    public static InvitationPageContent For(InvitationSetupOptions options, CultureInfo culture)
    {
        foreach (var candidate in new[] { culture.Name, culture.TwoLetterISOLanguageName, "en", "en-US" })
        {
            var match = options.Content.FirstOrDefault(entry => string.Equals(entry.Key, candidate, StringComparison.OrdinalIgnoreCase));
            if (match.Value is { } content)
            {
                return new(
                    content.Title ?? string.Empty,
                    content.Intro ?? string.Empty,
                    content.CompletionMessage ?? string.Empty,
                    content.SupportUrl ?? string.Empty);
            }
        }

        return new(string.Empty, string.Empty, string.Empty, string.Empty);
    }
}
