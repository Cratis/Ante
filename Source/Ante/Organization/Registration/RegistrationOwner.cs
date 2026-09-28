// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.IdentityProviders;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Microsoft.AspNetCore.Http;

namespace Ante.Organization.Registration;

/// <summary>
/// The subject of the person who started self-service registration.
/// </summary>
/// <param name="Value">The forwarded identity subject.</param>
[PII]
[ComplianceDetails("Identifies the person who owns a self-service registration for access control")]
public record RegistrationOwnerSubject(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Converts a forwarded subject to a registration owner subject.
    /// </summary>
    /// <param name="value">The subject value.</param>
    public static explicit operator RegistrationOwnerSubject(string value) => new(value);
}

/// <summary>
/// The login identity that owns a self-service registration.
/// </summary>
/// <param name="Subject">The forwarded sign-in subject.</param>
/// <param name="Provider">The resolved identity provider.</param>
public record RegistrationOwner(RegistrationOwnerSubject Subject, IdentityProviderName Provider)
{
    /// <summary>
    /// Resolves the current login using the same forwarded values used for the published registration.
    /// </summary>
    /// <param name="httpContextAccessor">Accessor for the current request.</param>
    /// <param name="identityProviderResolver">Resolver of the forwarded identity provider.</param>
    /// <returns>The current login, or null when there is no subject.</returns>
    public static RegistrationOwner? Resolve(IHttpContextAccessor httpContextAccessor, IIdentityProviderResolver identityProviderResolver)
    {
        var subject = ForwardedIdentitySubject.Resolve(httpContextAccessor);
        return string.IsNullOrWhiteSpace(subject)
            ? null
            : new((RegistrationOwnerSubject)subject, ForwardedIdentityProvider.Resolve(httpContextAccessor, identityProviderResolver));
    }
}

/// <summary>
/// Records the owner alongside a self-service registration; never sent to the host outbox.
/// </summary>
/// <param name="OwnerSubject">The person who registered.</param>
/// <param name="OwnerProvider">The identity provider for that login.</param>
[EventType]
public record RegistrationOwnerRecorded(RegistrationOwnerSubject OwnerSubject, IdentityProviderName OwnerProvider);
