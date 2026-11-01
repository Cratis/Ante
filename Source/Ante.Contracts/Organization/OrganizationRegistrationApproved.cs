// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Contracts.Organization;

/// <summary>
/// Appended by the host to its own outbox when a reviewer approves a sign-up that Ante is holding for review
/// (<c language="csharp">Ante:Registration:Review</c>). The registration id is the event source id. Ante then completes the
/// registration exactly as it would have without review and publishes <see cref="OrganizationRegistrationCompleted"/>.
/// </summary>
/// <remarks>
/// Ignored when the registration is not held, was already completed or was declined, so redelivery is safe.
/// </remarks>
[EventType]
public record OrganizationRegistrationApproved;
