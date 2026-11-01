// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Organization;

namespace Ante.Organization.Registration.Review;

/// <summary>
/// Recorded instead of <see cref="OrganizationRegistrationRecorded"/> when a sign-up is held for review
/// (<c language="csharp">Ante:Registration:Review</c>). It carries everything the registration needs, so an approval completes it
/// without asking the person again. Nothing is published to the host while a registration is only held.
/// </summary>
/// <param name="TenantName">The name of the organization to create.</param>
/// <param name="Subject">The subject of the signed-in user who registered.</param>
/// <param name="IdentityProvider">The identity provider the user signed in with.</param>
/// <param name="FirstName">The first name of the registering user.</param>
/// <param name="MiddleName">The middle name of the registering user.</param>
/// <param name="LastName">The last name of the registering user.</param>
/// <param name="Email">The email address of the registering user.</param>
/// <param name="SignupContext">The allowlisted signup context.</param>
[EventType]
public record OrganizationRegistrationHeld(
    TenantName TenantName,
    string Subject,
    IdentityProviderName IdentityProvider,
    FirstName FirstName,
    MiddleName MiddleName,
    LastName LastName,
    Email Email,
    IReadOnlyList<SignupContextEntry> SignupContext)
{
    /// <summary>
    /// Gets the completed registration an approval records.
    /// </summary>
    /// <returns>The registration as it would have been recorded without review.</returns>
    public OrganizationRegistrationRecorded ToRecorded() => new(
        TenantName, Subject, IdentityProvider, FirstName, MiddleName, LastName, Email, SignupContext);

    /// <summary>
    /// Gets the public outcome that tells the host a reviewer is needed.
    /// </summary>
    /// <returns>The event the host observes.</returns>
    public OrganizationRegistrationAwaitingReview ToContract() => new(
        TenantName, Subject, IdentityProvider, FirstName, MiddleName, LastName, Email, SignupContext);
}

/// <summary>
/// Recorded when a reviewer declines a held sign-up. Forwarded to the outbox as <see cref="OrganizationRegistrationDeclined"/>.
/// </summary>
/// <param name="Email">The contact address the person gave.</param>
/// <param name="FirstName">The first name the person gave.</param>
/// <param name="MiddleName">The middle name the person gave.</param>
/// <param name="LastName">The last name the person gave.</param>
/// <param name="Message">What the reviewer tells the person.</param>
[EventType]
public record OrganizationRegistrationDeclineRecorded(Email Email, FirstName FirstName, MiddleName MiddleName, LastName LastName, ReviewerMessage Message)
{
    /// <summary>
    /// Gets the public outcome for this decline.
    /// </summary>
    /// <returns>The event the host observes.</returns>
    public OrganizationRegistrationDeclined ToContract() => new(Email, FirstName, MiddleName, LastName, Message);
}
