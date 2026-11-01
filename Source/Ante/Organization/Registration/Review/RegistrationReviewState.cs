// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Organization;
using Ante.Invitations;

namespace Ante.Organization.Registration.Review;

/// <summary>
/// Where a registration stands in review.
/// </summary>
public enum RegistrationReviewState
{
    /// <summary>The registration was not held: it completes without review, or was approved and completed.</summary>
    NotHeld = 0,

    /// <summary>The registration is waiting for a reviewer's decision.</summary>
    Held = 1,

    /// <summary>A reviewer declined the registration.</summary>
    Declined = 2,
}

/// <summary>
/// The review standing of a registration, with the organization name it asked for.
/// </summary>
/// <param name="State">Where the registration stands in review.</param>
/// <param name="OrganizationName">The requested organization; null when the registration was never held.</param>
public record RegistrationReview(RegistrationReviewState State, TenantName? OrganizationName)
{
    static readonly EventType[] _types =
    [
        typeof(OrganizationRegistrationHeld).GetEventType(),
        typeof(OrganizationRegistrationRecorded).GetEventType(),
        typeof(OrganizationRegistrationDeclineRecorded).GetEventType(),
    ];

    /// <summary>
    /// Decides the review standing from a registration's events.
    /// </summary>
    /// <param name="events">The registration's held, completed and declined events.</param>
    /// <returns>The review standing.</returns>
    public static RegistrationReview Decide(IEnumerable<object> events)
    {
        var all = events.ToArray();
        var held = all.OfType<OrganizationRegistrationHeld>().FirstOrDefault();
        if (held is null || all.Any(content => content is OrganizationRegistrationRecorded))
        {
            return new(RegistrationReviewState.NotHeld, null);
        }

        return new(all.Any(content => content is OrganizationRegistrationDeclineRecorded) ? RegistrationReviewState.Declined : RegistrationReviewState.Held, held.TenantName);
    }

    /// <summary>
    /// Reads the review standing of a registration from the authoritative event log.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="registrationId">The registration identifier.</param>
    /// <returns>The review standing.</returns>
    public static async Task<RegistrationReview> Read(IEventStore eventStore, InvitationId registrationId)
    {
        var history = await eventStore.EventLog.GetForEventSourceIdAndEventTypes((EventSourceId)registrationId.Value.ToString("D"), _types);
        return Decide((history ?? []).Select(entry => entry.Content));
    }
}
