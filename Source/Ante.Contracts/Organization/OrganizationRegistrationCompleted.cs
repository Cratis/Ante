// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Ante.Contracts.Invitations;

namespace Ante.Contracts.Organization;

/// <summary>
/// Event published to Ante's outbox when a user completes self-service registration and sets up a new
/// organization, without going through an invitation flow. The host observes this to provision the
/// tenant and the user, exactly as it would for <see cref="InvitationToCreateTenantAccepted"/>.
/// </summary>
/// <remarks>
/// Generation 2 adds <see cref="SignupContext"/>. A generation-1 payload - historical facts, or a host
/// reading an inbox Chronicle has not migrated - deserializes with an empty context.
/// </remarks>
[EventType(generation: 2)]
public record OrganizationRegistrationCompleted
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationRegistrationCompleted"/> class.
    /// </summary>
    /// <param name="TenantName">The name of the tenant (organization) to create.</param>
    /// <param name="Subject">The subject (<c language="csharp">sub</c>) claim of the authenticated user who registered.</param>
    /// <param name="IdentityProvider">The identity provider the user authenticated with (e.g. the <c language="csharp">iss</c> claim).</param>
    /// <param name="FirstName">The first name of the registering user.</param>
    /// <param name="MiddleName">The middle name of the registering user.</param>
    /// <param name="LastName">The last name of the registering user.</param>
    /// <param name="Email">The email address of the registering user; may be empty and is never verified.</param>
    /// <param name="SignupContext">Allowlisted, untrusted context carried from the registration link.</param>
    [JsonConstructor]
    public OrganizationRegistrationCompleted(
        TenantName TenantName,
        string Subject,
        IdentityProviderName IdentityProvider,
        FirstName FirstName,
        MiddleName MiddleName,
        LastName LastName,
        Email Email,
        IReadOnlyList<SignupContextEntry>? SignupContext)
    {
        this.TenantName = TenantName;
        this.Subject = Subject;
        this.IdentityProvider = IdentityProvider;
        this.FirstName = FirstName;
        this.MiddleName = MiddleName;
        this.LastName = LastName;
        this.Email = Email;
        this.SignupContext = SignupContext ?? [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationRegistrationCompleted"/> class without signup context.
    /// </summary>
    /// <param name="TenantName">The name of the tenant (organization) to create.</param>
    /// <param name="Subject">The subject (<c language="csharp">sub</c>) claim of the authenticated user who registered.</param>
    /// <param name="IdentityProvider">The identity provider the user authenticated with.</param>
    /// <param name="FirstName">The first name of the registering user.</param>
    /// <param name="MiddleName">The middle name of the registering user.</param>
    /// <param name="LastName">The last name of the registering user.</param>
    /// <param name="Email">The email address of the registering user; may be empty and is never verified.</param>
    public OrganizationRegistrationCompleted(
        TenantName TenantName,
        string Subject,
        IdentityProviderName IdentityProvider,
        FirstName FirstName,
        MiddleName MiddleName,
        LastName LastName,
        Email Email)
        : this(TenantName, Subject, IdentityProvider, FirstName, MiddleName, LastName, Email, null)
    {
    }

    /// <summary>Gets the name of the tenant (organization) to create.</summary>
    public TenantName TenantName { get; init; }

    /// <summary>Gets the subject (<c language="csharp">sub</c>) claim of the authenticated user who registered.</summary>
    public string Subject { get; init; }

    /// <summary>Gets the identity provider the user authenticated with.</summary>
    public IdentityProviderName IdentityProvider { get; init; }

    /// <summary>Gets the first name of the registering user.</summary>
    public FirstName FirstName { get; init; }

    /// <summary>Gets the middle name of the registering user.</summary>
    public MiddleName MiddleName { get; init; }

    /// <summary>Gets the last name of the registering user.</summary>
    public LastName LastName { get; init; }

    /// <summary>Gets the email address of the registering user; may be empty and is never verified.</summary>
    public Email Email { get; init; }

    /// <summary>
    /// Gets allowlisted context carried from the registration link (for example <c language="csharp">offer</c> or
    /// <c language="csharp">utm_source</c>). Untrusted: validate any offer server-side before acting on it.
    /// </summary>
    public IReadOnlyList<SignupContextEntry> SignupContext { get; init; }

    /// <summary>Deconstructs the registration including its signup context.</summary>
    /// <param name="TenantName">The tenant name.</param>
    /// <param name="Subject">The subject.</param>
    /// <param name="IdentityProvider">The identity provider.</param>
    /// <param name="FirstName">The first name.</param>
    /// <param name="MiddleName">The middle name.</param>
    /// <param name="LastName">The last name.</param>
    /// <param name="Email">The email address.</param>
    /// <param name="SignupContext">The signup context.</param>
    public void Deconstruct(
        out TenantName TenantName,
        out string Subject,
        out IdentityProviderName IdentityProvider,
        out FirstName FirstName,
        out MiddleName MiddleName,
        out LastName LastName,
        out Email Email,
        out IReadOnlyList<SignupContextEntry> SignupContext) =>
        (TenantName, Subject, IdentityProvider, FirstName, MiddleName, LastName, Email, SignupContext) =
        (this.TenantName, this.Subject, this.IdentityProvider, this.FirstName, this.MiddleName, this.LastName, this.Email, this.SignupContext);

    /// <summary>Deconstructs the registration without its signup context.</summary>
    /// <param name="TenantName">The tenant name.</param>
    /// <param name="Subject">The subject.</param>
    /// <param name="IdentityProvider">The identity provider.</param>
    /// <param name="FirstName">The first name.</param>
    /// <param name="MiddleName">The middle name.</param>
    /// <param name="LastName">The last name.</param>
    /// <param name="Email">The email address.</param>
    public void Deconstruct(
        out TenantName TenantName,
        out string Subject,
        out IdentityProviderName IdentityProvider,
        out FirstName FirstName,
        out MiddleName MiddleName,
        out LastName LastName,
        out Email Email) =>
        (TenantName, Subject, IdentityProvider, FirstName, MiddleName, LastName, Email) =
        (this.TenantName, this.Subject, this.IdentityProvider, this.FirstName, this.MiddleName, this.LastName, this.Email);
}

/// <summary>
/// Generation 1 of <see cref="OrganizationRegistrationCompleted"/>, before signup context existed.
/// </summary>
/// <param name="TenantName">The name of the tenant (organization) to create.</param>
/// <param name="Subject">The subject claim of the authenticated user who registered.</param>
/// <param name="IdentityProvider">The identity provider the user authenticated with.</param>
/// <param name="FirstName">The first name of the registering user.</param>
/// <param name="MiddleName">The middle name of the registering user.</param>
/// <param name="LastName">The last name of the registering user.</param>
/// <param name="Email">The email address of the registering user.</param>
[EventTypeGenerationFor<OrganizationRegistrationCompleted>(1)]
public record OrganizationRegistrationCompletedV1(
    TenantName TenantName,
    string Subject,
    IdentityProviderName IdentityProvider,
    FirstName FirstName,
    MiddleName MiddleName,
    LastName LastName,
    Email Email);
