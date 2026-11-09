// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Invitations;

namespace Ante.Contracts.Organization;

/// <summary>
/// Published to Ante's outbox, on the registration id, when a sign-up held for review is declined. This is a decision
/// about the person, so a host may answer it with a "your sign-up was declined" message to <see cref="Email"/>. It is
/// published once per registration and never for a registration that was completed.
/// </summary>
/// <remarks>
/// Not to be confused with <see cref="InvitationRejected"/>, which says an invitation did not match what the host allows.
/// </remarks>
/// <param name="Email">The contact address the person gave; may be empty and is never verified.</param>
/// <param name="FirstName">The first name the person gave.</param>
/// <param name="MiddleName">The middle name the person gave.</param>
/// <param name="LastName">The last name the person gave.</param>
/// <param name="Message">What the reviewer tells the person about the decision.</param>
[EventType]
public record OrganizationRegistrationDeclined(Email Email, FirstName FirstName, MiddleName MiddleName, LastName LastName, ReviewerMessage Message);
