// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Organization;
using Ante.IdentityProviders;
using Ante.Invitations;
using Ante.Invitations.Receiving;
using Ante.Organization.Registration;
using Ante.Resources;
using Cratis.Arc.Validation;
using Cratis.Types;

namespace Ante.Organization.Registration.Start;

/// <summary>
/// Records the authenticated owner before a self-service registration is submitted. This local fact is
/// never published to the host, and it does not consume the one-use onboarding attempt.
/// </summary>
/// <param name="OwnerSubject">The exact forwarded subject of the person starting registration.</param>
/// <param name="OwnerProvider">The resolved provider for that subject.</param>
[EventType]
[Unique(name: "OneRegistrationStart", message: "This registration belongs to another sign-in.")]
public record RegistrationStarted([property: Subject] RegistrationOwnerSubject OwnerSubject, IdentityProviderName OwnerProvider);

/// <summary>
/// Associates a client-generated registration id with its authenticated owner before showing the wizard.
/// </summary>
/// <param name="RegistrationId">The registration's event source identifier.</param>
[Command]
public record BeginRegistration(InvitationId RegistrationId)
{
    /// <summary>
    /// Records a start exactly once; only the same actor can retry an already started registration.
    /// </summary>
    /// <param name="httpContextAccessor">Accessor for the current forwarded sign-in.</param>
    /// <param name="resolver">The canonical identity-provider resolver.</param>
    /// <param name="eventStore">The authoritative local event log and read models.</param>
    /// <returns>An empty event list after a start is recorded or the same owner retries.</returns>
    /// <exception cref="RegistrationStartAppendFailed">The event log could not record the start.</exception>
    public async Task<Result<ValidationResult, IEnumerable<object>>> Handle(
        IHttpContextAccessor httpContextAccessor,
        IIdentityProviderResolver resolver,
        IEventStore eventStore)
    {
        var owner = RegistrationOwner.Resolve(httpContextAccessor, resolver);
        if (RegistrationId == InvitationId.NotSet || owner is null || string.IsNullOrWhiteSpace(owner.Provider.Value))
        {
            return ValidationResult.Error(Messages.Get("RegisterIdentityRequired"));
        }

        if (!await RegistrationSourceAvailability.IsAvailable(RegistrationId, eventStore))
        {
            return ValidationResult.Error(Messages.Get("AttemptAlreadySubmitted"), reasonDetail: OnboardingAttemptConstraintNames.OneUseAttempt);
        }

        var starts = await RegistrationStartHistory.For(RegistrationId, eventStore);
        if (starts.Length == 1 && starts[0].OwnerSubject == owner.Subject && starts[0].OwnerProvider == owner.Provider)
        {
            return Array.Empty<object>();
        }

        if (starts.Length != 0)
        {
            return ValidationResult.Error(Messages.Get("RegistrationOwnedByAnotherSignIn"));
        }

        // A second start can pass the history check before the first append commits. Append
        // here so only this command can interpret the OneRegistrationStart race after re-reading
        // the authoritative history; Arc's automatic append would return a failed command even
        // when the winning start belongs to this same actor.
        var append = await eventStore.EventLog.Append(
            (EventSourceId)RegistrationId.Value.ToString("D"), new RegistrationStarted(owner.Subject, owner.Provider));
        if (append.IsSuccess)
        {
            return Array.Empty<object>();
        }

        if (append.HasConstraintViolations && !append.HasErrors && !append.HasConcurrencyViolations &&
            append.ConstraintViolations.All(violation => violation.ConstraintName == "OneRegistrationStart") &&
            await RegistrationStartHistory.BelongsTo(RegistrationId, owner, eventStore))
        {
            return Array.Empty<object>();
        }

        if (append.HasErrors || append.HasConcurrencyViolations)
        {
            throw new RegistrationStartAppendFailed();
        }

        return ValidationResult.Error(Messages.Get("RegistrationOwnedByAnotherSignIn"));
    }
}

/// <summary>
/// Reads the event log rather than a potentially lagging projection, so a successful BeginRegistration
/// response immediately permits submission and a different actor cannot reuse that id before projection.
/// </summary>
public static class RegistrationStartHistory
{
    /// <summary>
    /// Gets the recorded starts for the given event source; an ambiguous history never grants authority.
    /// </summary>
    /// <param name="id">The registration id.</param>
    /// <param name="eventStore">The authoritative local event store.</param>
    /// <returns>Recorded starts, if any.</returns>
    public static async Task<RegistrationStarted[]> For(InvitationId id, IEventStore eventStore)
    {
        var history = await eventStore.EventLog.GetForEventSourceIdAndEventTypes(
            (EventSourceId)id.Value.ToString("D"), [typeof(RegistrationStarted).GetEventType()]);
        return [.. history.Select(entry => entry.Content).OfType<RegistrationStarted>()];
    }

    /// <summary>
    /// Determines whether the start exists and belongs to the current exact subject/provider pair.
    /// </summary>
    /// <param name="id">The registration id.</param>
    /// <param name="owner">The forwarded, resolved sign-in.</param>
    /// <param name="eventStore">The authoritative event store.</param>
    /// <returns>True only when exactly one matching start was recorded.</returns>
    public static async Task<bool> BelongsTo(InvitationId id, RegistrationOwner? owner, IEventStore eventStore)
    {
        if (owner is null || string.IsNullOrWhiteSpace(owner.Provider.Value)) return false;
        var starts = await For(id, eventStore);
        return starts.Length == 1 && starts[0].OwnerSubject == owner.Subject && starts[0].OwnerProvider == owner.Provider;
    }
}

/// <summary>The event log failed to record the registration start for a reason other than an ownership conflict.</summary>
public sealed class RegistrationStartAppendFailed() : Exception("Unable to record the registration start.");
