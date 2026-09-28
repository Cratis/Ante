// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Organization.Registration;

/// <summary>
/// Fails startup on registration settings Ante cannot honor, rather than silently ignoring them.
/// </summary>
public static class RegistrationOptionsValidator
{
    /// <summary>
    /// Validates the registration settings.
    /// </summary>
    /// <param name="options">The deployment's options.</param>
    /// <exception cref="RegistrationConfigurationInvalid">A setting is out of range or malformed.</exception>
    public static void Validate(AnteOptions options)
    {
        var registration = options.Registration;
        if (registration.MaxPerIdentity < 0)
        {
            throw new RegistrationConfigurationInvalid("Ante:Registration:MaxPerIdentity cannot be negative.");
        }

        if (registration.MaxPerIdentity > 0 && registration.Window <= TimeSpan.Zero)
        {
            throw new RegistrationConfigurationInvalid("Ante:Registration:Window must be positive when MaxPerIdentity is set.");
        }

        if (registration.RequestsPerMinutePerClient < 0)
        {
            throw new RegistrationConfigurationInvalid("Ante:Registration:RequestsPerMinutePerClient cannot be negative.");
        }

        if (registration.ContextKeys.Count > RegistrationOptions.MaximumContextKeys)
        {
            throw new RegistrationConfigurationInvalid($"Ante:Registration:ContextKeys allows at most {RegistrationOptions.MaximumContextKeys} keys.");
        }

        if (registration.ContextKeys.Any(key => !SignupContextRules.IsValidKey(key)) ||
            registration.ContextKeys.Distinct(StringComparer.Ordinal).Count() != registration.ContextKeys.Count)
        {
            throw new RegistrationConfigurationInvalid(
                "Ante:Registration:ContextKeys must be distinct and contain only letters, digits, '_', '-' or '.'.");
        }

        if (!string.IsNullOrEmpty(registration.ClosedUrl) && !SafeLinks.IsAllowed(registration.ClosedUrl))
        {
            throw new RegistrationConfigurationInvalid("Ante:Registration:ClosedUrl must be an https URL or a same-origin path.");
        }

        foreach (var (locale, content) in registration.Content)
        {
            if (!SafeLinks.IsAllowed(content.PricingUrl) || !SafeLinks.IsAllowed(content.LoginUrl))
            {
                throw new RegistrationConfigurationInvalid(
                    $"Ante:Registration:Content:{locale} links must be https URLs or same-origin paths.");
            }
        }
    }
}

/// <summary>
/// The exception that is thrown when registration configuration is invalid.
/// </summary>
/// <param name="message">What is wrong.</param>
public class RegistrationConfigurationInvalid(string message) : Exception(message);
