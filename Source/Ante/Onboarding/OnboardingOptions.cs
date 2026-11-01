// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Legal;

namespace Ante.Onboarding;

/// <summary>
/// The onboarding contract for running one Ante as the onboarding service of one Screenplay application,
/// bound from <c language="csharp">Ante:Onboarding</c>. <c language="csharp">cratis run</c> and Stage Manager produce it from the
/// model's <c language="csharp">onboarding</c> block and a Cratis environment, so nobody writes the equivalent
/// <c language="csharp">Ante:*</c> keys by hand. When <see cref="Enabled"/> is true, the contract is mapped onto those keys and
/// wins over them; see <see cref="OnboardingConfiguration"/>.
/// </summary>
public class OnboardingOptions
{
    /// <summary>The sign-up that creates a tenant for the person who registers.</summary>
    public const string CreateTenant = "CreateTenant";

    /// <summary>The sign-up that has people join an existing tenant by invitation only.</summary>
    public const string JoinTenant = "JoinTenant";

    /// <summary>
    /// Gets or sets whether the contract is applied. Everything else here is ignored while it is false.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the public base URL of this Ante, as the application's users reach it. Maps to
    /// <c language="csharp">Ante:Invitations:Token:PublicBaseUrl</c>.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the URL of the application people are sent to when onboarding completes. Maps to
    /// <c language="csharp">Ante:HostAppUrl</c>.
    /// </summary>
    public string? ApplicationUrl { get; set; }

    /// <summary>
    /// Gets or sets the Chronicle store of the application, the one host store whose outbox Ante observes.
    /// Maps to a single entry in <c language="csharp">Ante:HostStores</c>.
    /// </summary>
    public string? HostStore { get; set; }

    /// <summary>
    /// Gets or sets Ante's own Chronicle store. Maps to <c language="csharp">Ante:EventStore</c>; unset keeps the default.
    /// </summary>
    public string? EventStore { get; set; }

    /// <summary>
    /// Gets or sets Ante's fixed Chronicle namespace. Maps to <c language="csharp">Ante:Namespace</c>; unset keeps the default.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets whether sign-up creates a tenant (<see cref="CreateTenant"/>, the default) or people only
    /// join existing tenants by invitation (<see cref="JoinTenant"/>), which closes self-service registration.
    /// </summary>
    public string SignUp { get; set; } = CreateTenant;

    /// <summary>
    /// Gets or sets whether self-service registration is open. Maps to <c language="csharp">Ante:Registration:Enabled</c>; it is
    /// always closed when <see cref="SignUp"/> is <see cref="JoinTenant"/>.
    /// </summary>
    public bool RegistrationEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the invitation types the application allows: <c language="csharp">JoinTenant</c> and/or
    /// <c language="csharp">CreateTenant</c>. Unset allows both. An invitation of another type is rejected with
    /// <c language="csharp">InvitationTypeNotAllowed</c>.
    /// </summary>
    public string[]? InvitationTypes { get; set; }

    /// <summary>
    /// Gets or sets the personas an invitation may grant, as declared in the model. When set, an invitation
    /// granting a role outside the list is rejected with <c language="csharp">PersonaNotGrantable</c>. Unset or empty
    /// accepts any role, as before.
    /// </summary>
    public string[]? Personas { get; set; }

    /// <summary>
    /// Gets or sets the default locale. Maps to <c language="csharp">Ante:DefaultLocale</c>.
    /// </summary>
    public string? DefaultLocale { get; set; }

    /// <summary>
    /// Gets or sets the allowed locales. Maps to <c language="csharp">Ante:SupportedLocales</c>.
    /// </summary>
    public string[]? AllowedLocales { get; set; }

    /// <summary>
    /// Gets or sets the legal terms files. Maps to <c language="csharp">Ante:Legal:Source=Files</c> and
    /// <c language="csharp">Ante:Legal:Files</c>; unset leaves legal terms as they were.
    /// </summary>
    public LegalFilesOptions? Legal { get; set; }

    /// <summary>
    /// Gets or sets where secrets are mounted.
    /// </summary>
    public OnboardingSecretsOptions Secrets { get; set; } = new();

    /// <summary>
    /// Gets or sets the development mode used by <c language="csharp">cratis run</c>.
    /// </summary>
    public OnboardingDevelopmentOptions Development { get; set; } = new();
}

/// <summary>
/// Where Ante reads secrets from. Secrets are never accepted on the command line and never logged.
/// </summary>
public class OnboardingSecretsOptions
{
    /// <summary>
    /// Gets or sets a mounted folder with one file per setting, named after the configuration key with
    /// <c language="csharp">__</c> for <c language="csharp">:</c> (for example <c language="csharp">Ante__Invitations__Token__PrivateKeyPem</c>), as a Kubernetes
    /// secret volume provides. Each file's content is the value.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the path of a PEM file holding the invitation signing private key.
    /// </summary>
    public string? SigningKeyFile { get; set; }

    /// <summary>
    /// Gets or sets the path of a PEM file holding an additional trusted public verification key.
    /// </summary>
    public string? VerificationKeyFile { get; set; }
}

/// <summary>
/// The development mode for running the onboarding service locally.
/// </summary>
public class OnboardingDevelopmentOptions
{
    /// <summary>
    /// Gets or sets whether development mode is on. It is refused when the environment looks like production.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the folder, normally on the data volume, where the generated development signing key is kept
    /// between runs. Defaults to <c language="csharp">.ante-development</c> in the content root, which is ignored by Git.
    /// </summary>
    public string? DataPath { get; set; }
}
