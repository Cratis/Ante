// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Ante.Organization.Registration;

/// <summary>
/// Recorded once per completed self-service registration against a pseudonymous key for the sign-in that
/// registered, so the number of registrations one sign-in has made can be counted authoritatively.
/// </summary>
[EventType]
public record RegistrationQuotaConsumed;

/// <summary>
/// The outcome of checking a sign-in's registration quota.
/// </summary>
/// <param name="IsAllowed">Whether the sign-in may register another organization now.</param>
/// <param name="Key">The pseudonymous event source the consumption is recorded under.</param>
/// <param name="Scope">The concurrency scope that makes the recorded consumption race-safe.</param>
public record RegistrationQuotaCheck(bool IsAllowed, EventSourceId Key, ConcurrencyScope Scope);

/// <summary>
/// Counts self-service registrations per sign-in within a sliding window.
/// </summary>
/// <remarks>
/// The key is a SHA-256 hash of the identity provider and subject, so the event log never holds the
/// subject in clear under this stream. Every registration records a consumption, even when no limit is
/// configured, so turning a limit on later counts the history that already exists. The check returns a
/// concurrency scope at the key's tail: two concurrent registrations by the same sign-in cannot both pass
/// a limit of one, because the second append fails its scope and has to be retried against the new count.
/// </remarks>
public static class RegistrationQuota
{
    /// <summary>
    /// Gets the pseudonymous event source for a sign-in.
    /// </summary>
    /// <param name="owner">The registering sign-in.</param>
    /// <returns>The event source id consumption is recorded under.</returns>
    public static EventSourceId KeyFor(RegistrationOwner owner)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{owner.Provider.Value}\n{owner.Subject.Value}"));
        return new($"registration-owner-{Convert.ToHexStringLower(hash)}");
    }

    /// <summary>
    /// Checks whether the sign-in is within its limit, without reading anything when no limit is configured.
    /// </summary>
    /// <param name="eventStore">The authoritative local event store.</param>
    /// <param name="options">The registration options.</param>
    /// <param name="owner">The registering sign-in.</param>
    /// <param name="now">The current time.</param>
    /// <returns>True when the sign-in may register another organization.</returns>
    public static async Task<bool> IsWithinLimit(IEventStore eventStore, RegistrationOptions options, RegistrationOwner owner, DateTimeOffset now) =>
        options.MaxPerIdentity <= 0 || (await Check(eventStore, options, owner, now)).IsAllowed;

    /// <summary>
    /// Checks whether the sign-in may register another organization.
    /// </summary>
    /// <param name="eventStore">The authoritative local event store.</param>
    /// <param name="options">The registration options.</param>
    /// <param name="owner">The registering sign-in.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The check result, including the scope to append the consumption with.</returns>
    public static async Task<RegistrationQuotaCheck> Check(IEventStore eventStore, RegistrationOptions options, RegistrationOwner owner, DateTimeOffset now)
    {
        var key = KeyFor(owner);
        var history = await eventStore.EventLog.GetForEventSourceIdAndEventTypes(key, [typeof(RegistrationQuotaConsumed).GetEventType()]);
        var tail = history.Count > 0 ? history[^1].Context.SequenceNumber : EventSequenceNumber.BeforeFirst;
        var scope = new ConcurrencyScope(tail, key, EventTypes: [typeof(RegistrationQuotaConsumed).GetEventType()]);
        if (options.MaxPerIdentity <= 0)
        {
            return new(true, key, scope);
        }

        var since = now - options.Window;
        var used = history.Count(entry => entry.Context.Occurred >= since);
        return new(used < options.MaxPerIdentity, key, scope);
    }
}
