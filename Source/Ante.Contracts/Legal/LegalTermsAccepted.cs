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
/// <param name="TenantName">
/// The organization the acceptance was given for. It travels in the payload because the acceptance is
/// appended to Ante's single-tenant outbox, which cannot carry the organization in its namespace.
/// </param>
/// <param name="IdentityProvider">The identity provider the accepting user authenticated with.</param>
/// <param name="IdentityProviderSubject">
/// The stable subject the identity provider issued for the accepting user. Carried instead of the email
/// address so no personal data is copied into the host's compliance record, and it is the same value
/// the host keys its own user registry off, so it can correlate the acceptance with a user later.
/// </param>
/// <param name="Version">The version of the legal document set that was presented and accepted.</param>
[EventType]
public record LegalTermsAccepted(TenantName TenantName, IdentityProviderName IdentityProvider, string IdentityProviderSubject, LegalVersion Version);
