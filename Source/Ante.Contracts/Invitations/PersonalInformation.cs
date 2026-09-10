// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Concepts;

namespace Ante.Contracts.Invitations;

/// <summary>
/// Represents the email address of a person going through an invitation or registration flow.
/// </summary>
/// <param name="Value">The underlying value.</param>
[PII]
public record Email(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="Email"/>.
    /// </summary>
    public static readonly Email NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to an <see cref="Email"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator Email(string value) => new(value);

    /// <summary>
    /// Implicitly converts an <see cref="Email"/> to its underlying <see cref="string"/> value.
    /// </summary>
    /// <param name="email">The value to convert from.</param>
    public static implicit operator string(Email email) => email.Value;
}

/// <summary>
/// Represents the first name of a person going through an invitation or registration flow.
/// </summary>
/// <param name="Value">The underlying value.</param>
[PII]
public record FirstName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="FirstName"/>.
    /// </summary>
    public static readonly FirstName NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="FirstName"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator FirstName(string value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="FirstName"/> to its underlying <see cref="string"/> value.
    /// </summary>
    /// <param name="name">The value to convert from.</param>
    public static implicit operator string(FirstName name) => name.Value;
}

/// <summary>
/// Represents the middle name of a person going through an invitation or registration flow.
/// </summary>
/// <param name="Value">The underlying value.</param>
[PII]
public record MiddleName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="MiddleName"/>.
    /// </summary>
    public static readonly MiddleName NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="MiddleName"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator MiddleName(string value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="MiddleName"/> to its underlying <see cref="string"/> value.
    /// </summary>
    /// <param name="name">The value to convert from.</param>
    public static implicit operator string(MiddleName name) => name.Value;
}

/// <summary>
/// Represents the last name of a person going through an invitation or registration flow.
/// </summary>
/// <param name="Value">The underlying value.</param>
[PII]
public record LastName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="LastName"/>.
    /// </summary>
    public static readonly LastName NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="LastName"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator LastName(string value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="LastName"/> to its underlying <see cref="string"/> value.
    /// </summary>
    /// <param name="name">The value to convert from.</param>
    public static implicit operator string(LastName name) => name.Value;
}

/// <summary>
/// Represents the name of the tenant (organization) an invitation or registration relates to.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record TenantName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="TenantName"/>.
    /// </summary>
    public static readonly TenantName NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="TenantName"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator TenantName(string value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="TenantName"/> to its underlying <see cref="string"/> value.
    /// </summary>
    /// <param name="name">The value to convert from.</param>
    public static implicit operator string(TenantName name) => name.Value;
}

/// <summary>
/// Represents the name of the identity provider a person authenticated with (e.g. resolved from the
/// <c language="csharp">iss</c> claim). Named with the <c language="csharp">Name</c> suffix - like its sibling concepts <see cref="TenantName"/>
/// and <see cref="RoleName"/> - because <c language="csharp">Cratis.Arc.Identity</c> already defines its own unrelated
/// <c language="csharp">IdentityProvider</c> type (an authentication-scheme marker), and the two would otherwise collide
/// wherever both are in scope.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record IdentityProviderName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="IdentityProviderName"/>.
    /// </summary>
    public static readonly IdentityProviderName NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to an <see cref="IdentityProviderName"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator IdentityProviderName(string value) => new(value);

    /// <summary>
    /// Implicitly converts an <see cref="IdentityProviderName"/> to its underlying <see cref="string"/> value.
    /// </summary>
    /// <param name="provider">The value to convert from.</param>
    public static implicit operator string(IdentityProviderName provider) => provider.Value;
}

/// <summary>
/// Represents the name of a role a user is being invited into.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record RoleName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="RoleName"/>.
    /// </summary>
    public static readonly RoleName NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="RoleName"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator RoleName(string value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="RoleName"/> to its underlying <see cref="string"/> value.
    /// </summary>
    /// <param name="role">The value to convert from.</param>
    public static implicit operator string(RoleName role) => role.Value;
}

/// <summary>
/// Represents the identifier a host product assigns to an organization it provisions after accepting
/// an invitation or a self-service registration.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record OrganizationId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="OrganizationId"/>.
    /// </summary>
    public static readonly OrganizationId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="Guid"/> to an <see cref="OrganizationId"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator OrganizationId(Guid value) => new(value);

    /// <summary>
    /// Creates a new <see cref="OrganizationId"/> with a random value.
    /// </summary>
    /// <returns>A new <see cref="OrganizationId"/>.</returns>
    public static OrganizationId New() => new(Guid.NewGuid());
}
