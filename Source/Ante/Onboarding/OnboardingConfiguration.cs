// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Legal;
using Microsoft.Extensions.Configuration;

namespace Ante.Onboarding;

/// <summary>
/// Maps the onboarding contract (<c language="csharp">Ante:Onboarding</c>) onto Ante's existing <c language="csharp">Ante:*</c> settings, so a
/// Screenplay application's onboarding service needs no hand-written Ante configuration. The existing settings
/// keep working unchanged when the contract is not enabled.
/// </summary>
public static class OnboardingConfiguration
{
    /// <summary>The configuration section the contract is bound from.</summary>
    public const string Section = "Ante:Onboarding";

    static readonly string[] _invitationTypes = [OnboardingOptions.JoinTenant, OnboardingOptions.CreateTenant];

    /// <summary>
    /// Adds the settings the contract implies to the configuration, above every other source. Does nothing when the
    /// contract is not enabled.
    /// </summary>
    /// <param name="configuration">The application configuration; mounted secrets are added to it too.</param>
    /// <param name="environment">The hosting environment, used to refuse development mode in production.</param>
    /// <exception cref="OnboardingConfigurationInvalid">The contract is inconsistent or development mode is refused.</exception>
    public static void Apply(IConfigurationManager configuration, IHostEnvironment environment)
    {
        var options = configuration.GetSection(Section).Get<OnboardingOptions>() ?? new OnboardingOptions();
        if (!options.Enabled)
        {
            return;
        }

        Validate(options);
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        AddMountedSecrets(options, values);
        Translate(options, values);

        if (options.Development.Enabled)
        {
            OnboardingDevelopment.Refuse(options, environment);
            var dataPath = options.Development.DataPath ?? Path.Combine(environment.ContentRootPath, OnboardingDevelopment.DefaultFolder);
            values["Ante:Invitations:Token:PrivateKeyPem"] = OnboardingDevelopment.LoadOrCreateSigningKey(dataPath);
            values.TryAdd("Ante:HostStores:0", "Application");
        }

        configuration.AddInMemoryCollection(values);
    }

    /// <summary>
    /// Checks the contract is consistent before anything is mapped.
    /// </summary>
    /// <param name="options">The contract.</param>
    /// <exception cref="OnboardingConfigurationInvalid">The contract is inconsistent.</exception>
    public static void Validate(OnboardingOptions options)
    {
        if (options.SignUp is not (OnboardingOptions.CreateTenant or OnboardingOptions.JoinTenant))
        {
            throw new OnboardingConfigurationInvalid("Ante:Onboarding:SignUp must be CreateTenant or JoinTenant.");
        }

        if (options.InvitationTypes is { } types && (types.Length == 0 || types.Any(type => !_invitationTypes.Contains(type, StringComparer.Ordinal))))
        {
            throw new OnboardingConfigurationInvalid("Ante:Onboarding:InvitationTypes must list JoinTenant and/or CreateTenant.");
        }

        if (options.Personas is { } personas && personas.Any(string.IsNullOrWhiteSpace))
        {
            throw new OnboardingConfigurationInvalid("Ante:Onboarding:Personas must not contain blank names.");
        }

        if (!options.Development.Enabled && string.IsNullOrWhiteSpace(options.HostStore))
        {
            throw new OnboardingConfigurationInvalid("Ante:Onboarding:HostStore must name the application's Chronicle store.");
        }

        if (options.Secrets.SigningKeyFile is not null && !File.Exists(options.Secrets.SigningKeyFile))
        {
            throw new OnboardingConfigurationInvalid("Ante:Onboarding:Secrets:SigningKeyFile does not exist.");
        }

        if (options.Secrets.VerificationKeyFile is not null && !File.Exists(options.Secrets.VerificationKeyFile))
        {
            throw new OnboardingConfigurationInvalid("Ante:Onboarding:Secrets:VerificationKeyFile does not exist.");
        }
    }

    /// <summary>
    /// Maps the contract onto the <c language="csharp">Ante:*</c> keys it stands for.
    /// </summary>
    /// <param name="options">The validated contract.</param>
    /// <param name="values">Receives the keys and values; secrets are read from their files here and never logged.</param>
    public static void Translate(OnboardingOptions options, IDictionary<string, string?> values)
    {
        Set(values, "Ante:EventStore", options.EventStore);
        Set(values, "Ante:Namespace", options.Namespace);
        Set(values, "Ante:HostAppUrl", options.ApplicationUrl);
        Set(values, "Ante:Invitations:Token:PublicBaseUrl", options.PublicBaseUrl);
        Set(values, "Ante:DefaultLocale", options.DefaultLocale);
        if (!string.IsNullOrWhiteSpace(options.HostStore))
        {
            values["Ante:HostStores:0"] = options.HostStore;
        }

        values["Ante:Registration:Enabled"] = (options.RegistrationEnabled && options.SignUp == OnboardingOptions.CreateTenant).ToString();
        if (options.RegistrationReview)
        {
            values["Ante:Registration:Review"] = bool.TrueString;
        }

        for (var index = 0; index < options.AllowedLocales?.Length; index++)
        {
            values[$"Ante:SupportedLocales:{index}"] = options.AllowedLocales[index];
        }

        if (options.Legal is { } legal)
        {
            values["Ante:Legal:Source"] = "Files";
            Set(values, "Ante:Legal:Files:Folder", legal.Folder);
            Set(values, "Ante:Legal:Files:TermsAndConditions", legal.TermsAndConditions);
            Set(values, "Ante:Legal:Files:PrivacyPolicy", legal.PrivacyPolicy);
        }

        if (options.Secrets.SigningKeyFile is { } signing)
        {
            values["Ante:Invitations:Token:PrivateKeyPem"] = File.ReadAllText(signing);
        }

        if (options.Secrets.VerificationKeyFile is { } verification)
        {
            values["Ante:Invitations:Token:PublicKeyPem"] = File.ReadAllText(verification);
        }
    }

    static void AddMountedSecrets(OnboardingOptions options, Dictionary<string, string?> values)
    {
        if (string.IsNullOrWhiteSpace(options.Secrets.Path) || !Directory.Exists(options.Secrets.Path))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(options.Secrets.Path).Where(path => !Path.GetFileName(path).StartsWith('.')))
        {
            values[Path.GetFileName(file).Replace("__", ":", StringComparison.Ordinal)] = File.ReadAllText(file).TrimEnd('\r', '\n');
        }
    }

    static void Set(IDictionary<string, string?> values, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            values[key] = value;
        }
    }
}

/// <summary>
/// The exception that is thrown when the onboarding contract is inconsistent or may not run here.
/// </summary>
/// <param name="message">A diagnostic naming the setting; it never includes a secret value.</param>
public class OnboardingConfigurationInvalid(string message) : Exception(message);
