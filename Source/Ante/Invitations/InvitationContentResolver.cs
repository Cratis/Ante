// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Ante.Configuration;

namespace Ante.Invitations;

/// <summary>Resolves one invitation journey's copy without consulting registration settings.</summary>
public static class InvitationContentResolver
{
    /// <summary>Resolves exact locale, neutral language, then English; absent fields retain frontend defaults.</summary>
    /// <param name="options">The journey's configured content.</param>
    /// <param name="culture">The negotiated request culture.</param>
    /// <returns>Only allowlisted, public presentation fields.</returns>
    public static InvitationPageContent For(InvitationSetupOptions options, CultureInfo culture)
    {
        InvitationContent? Find(string locale) => options.Content
            .FirstOrDefault(entry => string.Equals(entry.Key, locale, StringComparison.OrdinalIgnoreCase)).Value;

        var content = Find(culture.Name) ?? Find(culture.TwoLetterISOLanguageName) ?? Find("en");
        return new(
            content?.Title ?? string.Empty,
            content?.Intro ?? string.Empty,
            content?.CompletionMessage ?? string.Empty,
            content?.SupportUrl ?? string.Empty);
    }
}
