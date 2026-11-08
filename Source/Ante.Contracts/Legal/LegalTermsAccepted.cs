// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Invitations;

namespace Ante.Contracts.Legal;

/// <summary>
/// Event published to Ante's outbox when a user accepts the legal document set a host supplied through
/// its <c language="csharp">ILegalDocumentSource</c>, while onboarding through Ante - whether by self-service
/// registration, by setting up a new organization from an invitation, or by joining an existing one.
/// Only appended when the host has actually registered a legal document source; a host that has not
/// never sees this event, because the wizards never presented a terms step to accept.
/// </summary>
/// <remarks>
/// Generation 2 adds <see cref="IdentityProviderKey"/> and <see cref="IdentityIssuer"/>. A generation-1 payload - historical
/// facts, or a host reading an inbox Chronicle has not migrated - deserializes with both empty.
/// </remarks>
/// <param name="TenantName">
/// The organization the acceptance was given for. It travels in the payload because the acceptance is
/// appended to Ante's single-tenant outbox, which cannot carry the organization in its namespace.
/// </param>
/// <param name="IdentityProvider">The name of the identity provider the accepting user authenticated with. This is the provider name, not the issuer; see <c language="csharp">IdentityIssuer</c>.</param>
/// <param name="IdentityProviderSubject">
/// The stable subject the identity provider issued for the accepting user. Carried instead of the email
/// address so no personal data is copied into the host's compliance record, and it is the same value
/// the host keys its own user registry off, so it can correlate the acceptance with a user later.
/// </param>
/// <param name="Version">The version of the legal document set that was presented and accepted.</param>
[EventType(generation: 2)]
public record LegalTermsAccepted(TenantName TenantName, IdentityProviderName IdentityProvider, string IdentityProviderSubject, LegalVersion Version)
{
    /// <summary>
    /// Gets the canonical provider key of the identity provider the accepting user authenticated with (the registration key
    /// Ante resolved, not its display name). Empty when unknown, as for facts recorded before this field existed.
    /// </summary>
    public string IdentityProviderKey { get; init; } = string.Empty;

    /// <summary>
    /// Gets the issuer (<c language="csharp">iss</c>) of the identity provider the accepting user authenticated with. Together with
    /// <c language="csharp">IdentityProviderSubject</c> it identifies the federated identity without a provider-name lookup.
    /// Empty when unknown, as for facts recorded before this field existed; never guessed.
    /// </summary>
    public string IdentityIssuer { get; init; } = string.Empty;
}

/// <summary>Generation 1 of <see cref="LegalTermsAccepted"/>, before the identity provider key and issuer were carried.</summary>
/// <param name="TenantName">
/// The organization the acceptance was given for. It travels in the payload because the acceptance is
/// appended to Ante's single-tenant outbox, which cannot carry the organization in its namespace.
/// </param>
/// <param name="IdentityProvider">The name of the identity provider the accepting user authenticated with. This is the provider name, not the issuer; see <c language="csharp">IdentityIssuer</c>.</param>
/// <param name="IdentityProviderSubject">
/// The stable subject the identity provider issued for the accepting user. Carried instead of the email
/// address so no personal data is copied into the host's compliance record, and it is the same value
/// the host keys its own user registry off, so it can correlate the acceptance with a user later.
/// </param>
/// <param name="Version">The version of the legal document set that was presented and accepted.</param>
[EventTypeGenerationFor<LegalTermsAccepted>(1)]
public record LegalTermsAcceptedV1(TenantName TenantName, IdentityProviderName IdentityProvider, string IdentityProviderSubject, LegalVersion Version);
