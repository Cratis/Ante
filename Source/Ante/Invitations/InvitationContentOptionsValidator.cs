// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations;

/// <summary>Rejects invalid host-authored invitation copy before serving requests.</summary>
public static class InvitationContentOptionsValidator
{
    static readonly HashSet<string> _contentKeys = [
        with(StringComparer.OrdinalIgnoreCase),
        nameof(InvitationContent.Title),
        nameof(InvitationContent.Intro),
        nameof(InvitationContent.CompletionMessage),
        nameof(InvitationContent.SupportUrl),
    ];

    /// <summary>Validates each invitation journey independently of self-registration.</summary>
    /// <param name="options">The bound Ante options.</param>
    /// <param name="configuration">The Ante configuration section, used to reject unknown content keys.</param>
    public static void Validate(AnteOptions options, IConfigurationSection configuration)
    {
        ValidateJourney(options.Invitations.UserSetup, configuration.GetSection("Invitations:UserSetup:Content"));
        ValidateJourney(options.Invitations.OrganizationSetup, configuration.GetSection("Invitations:OrganizationSetup:Content"));
    }

    static void ValidateJourney(InvitationSetupOptions options, IConfigurationSection section)
    {
        ValidateKeys(section);
        foreach (var (locale, content) in options.Content)
        {
            ValidateContent(content, $"{section.Path}:{locale}");
        }
    }

    static void ValidateKeys(IConfigurationSection section)
    {
        foreach (var field in section.GetChildren().SelectMany(locale => locale.GetChildren()))
        {
            if (!_contentKeys.Contains(field.Key) || field.GetChildren().Any())
            {
                throw new InvitationContentConfigurationInvalid($"{field.Path} must be an allowlisted plain-text invitation content field.");
            }
        }
    }

    static void ValidateContent(InvitationContent content, string path)
    {
        if (new[] { content.Title, content.Intro, content.CompletionMessage }.Any(value => value?.Length > 10000))
        {
            throw new InvitationContentConfigurationInvalid($"{path} text is too long (maximum 10000 characters per field).");
        }

        if (!string.IsNullOrEmpty(content.SupportUrl) && !IsHttpsUrl(content.SupportUrl))
        {
            throw new InvitationContentConfigurationInvalid($"{path}:SupportUrl must be an absolute HTTPS URL or empty.");
        }
    }

    static bool IsHttpsUrl(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps && !string.IsNullOrEmpty(uri.Host) && uri.IsWellFormedOriginalString();
}
