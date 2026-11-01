// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Contracts.Organization;

/// <summary>
/// Appended by the host to its own outbox when a reviewer declines a sign-up that Ante is holding for review
/// (<c language="csharp">Ante:Registration:Review</c>). The registration id is the event source id. Ante answers with
/// <see cref="OrganizationRegistrationDeclined"/>.
/// </summary>
/// <remarks>
/// Ignored when the registration is not held, was already completed or was already declined, so redelivery is safe.
/// A blank message is refused: the person is told why, so the reviewer has to say.
/// </remarks>
/// <param name="Message">What the reviewer tells the person about the decision.</param>
[EventType]
public record OrganizationRegistrationDeclineRequested(ReviewerMessage Message);
