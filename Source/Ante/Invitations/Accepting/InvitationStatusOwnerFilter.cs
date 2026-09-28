// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;

namespace Ante.Invitations.Accepting;

/// <summary>
/// Gives each observable status subscriber its own immutable actor binding. A shared invitation
/// subject cannot authorize a subscriber: another actor may complete and accept the invitation
/// after both subscriptions were opened.
/// </summary>
/// <typeparam name="T">The status view.</typeparam>
/// <param name="source">The shared invitation status stream.</param>
/// <param name="invitationId">The invitation bound to this subscriber.</param>
/// <param name="actor">The captured canonical actor.</param>
/// <param name="eventStore">The authoritative local owner record.</param>
/// <param name="isPending">Identifies the public, non-terminal pending value.</param>
public sealed class InvitationStatusOwnerFilter<T>(
    ISubject<T> source,
    InvitationId invitationId,
    InvitedAcceptanceOwnerRecorded actor,
    IEventStore eventStore,
    Func<T, bool> isPending) : ISubject<T>
{
    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<T> observer) => source.Where(Allowed).Subscribe(observer);

    /// <inheritdoc/>
    public void OnNext(T value) => source.OnNext(value);

    /// <inheritdoc/>
    public void OnError(Exception error) => source.OnError(error);

    /// <inheritdoc/>
    public void OnCompleted() => source.OnCompleted();

    bool Allowed(T value)
    {
        if (isPending(value))
        {
            return true;
        }

        // Acceptance and its owner are appended in the same batch. On every emission, including
        // the first BehaviorSubject snapshot, consult the authoritative log rather than a projection
        // or the request-scoped identity that may now belong to another HTTP request.
        var history = eventStore.EventLog.GetForEventSourceIdAndEventTypes(
            (EventSourceId)invitationId.Value.ToString("D"),
            [typeof(InvitedAcceptanceOwnerRecorded).GetEventType()]).GetAwaiter().GetResult();
        var owners = history.Select(entry => entry.Content).OfType<InvitedAcceptanceOwnerRecorded>().ToArray();
        return owners.Length == 1 && owners[0] == actor;
    }
}
