// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Invitations;

namespace Ante.Contracts.Organization;

/// <summary>
/// Published to Ante's outbox, on the registration id, when a sign-up is held for review
/// (<c language="csharp">Ante:Registration:Review</c>). It carries what a reviewer needs to decide, and what
/// <see cref="OrganizationRegistrationCompleted"/> would carry if the registration were approved. The host answers by
/// appending <see cref="OrganizationRegistrationApproved"/> or <see cref="OrganizationRegistrationDeclineRequested"/>
/// to its own outbox under the same event source id.
/// </summary>
/// <remarks>
/// Published once per registration, and only while review is on. Nothing else is published for the registration until a decision.
/// </remarks>
/// <param name="TenantName">The name of the organization (tenant) the person asked for.</param>
/// <param name="Subject">The subject (<c language="csharp">sub</c>) claim of the authenticated user who registered.</param>
/// <param name="IdentityProvider">The identity provider the user authenticated with.</param>
/// <param name="FirstName">The first name the person gave.</param>
/// <param name="MiddleName">The middle name the person gave.</param>
/// <param name="LastName">The last name the person gave.</param>
/// <param name="Email">The contact address the person gave; may be empty and is never verified.</param>
/// <param name="SignupContext">Allowlisted, untrusted context carried from the registration link.</param>
[EventType]
public record OrganizationRegistrationAwaitingReview(
    TenantName TenantName,
    string Subject,
    IdentityProviderName IdentityProvider,
    FirstName FirstName,
    MiddleName MiddleName,
    LastName LastName,
    Email Email,
    IReadOnlyList<SignupContextEntry> SignupContext);
