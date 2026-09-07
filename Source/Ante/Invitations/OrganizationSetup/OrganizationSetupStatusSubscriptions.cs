// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Ante.Invitations.OrganizationSetup;

/// <summary>
/// Tracks organization setup acceptance status streams per invitation.
/// </summary>
public class OrganizationSetupStatusSubscriptions : IDisposable
{
    static readonly TimeSpan _defaultEntryRetention = TimeSpan.FromMinutes(2);

    readonly ConcurrentDictionary<InvitationId, BehaviorSubject<OrganizationSetupAcceptanceStatusView>> _subscriptions = new();
    readonly ConcurrentDictionary<InvitationId, DateTimeOffset> _acceptedAt = new();
    readonly Timer _cleanupTimer;
    readonly TimeSpan _entryRetention;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationSetupStatusSubscriptions"/> class.
    /// </summary>
    /// <param name="cleanupInterval">Optional cleanup interval override.</param>
    /// <param name="entryRetention">Optional terminal entry retention override.</param>
    public OrganizationSetupStatusSubscriptions(TimeSpan? cleanupInterval = null, TimeSpan? entryRetention = null)
    {
        _entryRetention = entryRetention ?? _defaultEntryRetention;

        var effectiveCleanupInterval = cleanupInterval ?? TimeSpan.FromSeconds(30);
        _cleanupTimer = new(_ => Cleanup(), null, effectiveCleanupInterval, effectiveCleanupInterval);
    }

    /// <summary>
    /// Gets the status stream for an invitation, seeded from durable evidence so a re-entering client
    /// never observes a stale value.
    /// </summary>
    /// <remarks>
    /// Seeding keeps re-entry idempotent: a fresh in-memory entry otherwise starts out
    /// <see cref="OrganizationSetupAcceptanceStatus.Pending"/>, so without the durable check a user
    /// returning after a restart - or reconnecting to a different replica - would see a stale pending
    /// state even though setup was already recorded or fully published. An already-live subject is only
    /// ever moved forward (Pending -&gt; Recorded -&gt; Accepted), never backward, so a slow reconnect's
    /// durable read cannot regress a state another tab watching the same subject has already observed.
    /// </remarks>
    /// <param name="invitationId">The invitation identifier.</param>
    /// <param name="organizationName">The name of the organization that was set up, when known.</param>
    /// <param name="isRecorded">Whether durable evidence confirms setup has been recorded.</param>
    /// <param name="isFullyPublished">Whether durable evidence already confirms full publication.</param>
    /// <returns>An observable status stream.</returns>
    public ISubject<OrganizationSetupAcceptanceStatusView> GetStatus(
        InvitationId invitationId,
        TenantName? organizationName = null,
        bool isRecorded = false,
        bool isFullyPublished = false)
    {
        var subject = GetOrAdd(invitationId);

        if (isFullyPublished && organizationName is not null)
        {
            MarkAccepted(invitationId, organizationName);
        }
        else if (isRecorded && organizationName is not null && subject.Value.Status != OrganizationSetupAcceptanceStatus.Accepted)
        {
            // Never regresses an already-Accepted subject: a stale read of the recorded collection racing
            // behind a durable publication another caller already observed must not un-accept a subject a
            // different tab is watching right now.
            MarkRecorded(invitationId, organizationName);
        }

        return subject;
    }

    /// <summary>
    /// Marks an invitation as recorded. Called once durable evidence confirms setup has committed to
    /// Ante's own event log, so a subscriber already waiting on this subject learns it must not
    /// resubmit, without needing to reconnect first.
    /// </summary>
    /// <param name="invitationId">The invitation identifier.</param>
    /// <param name="organizationName">The name of the organization that was set up.</param>
    public void MarkRecorded(InvitationId invitationId, TenantName organizationName)
    {
        var subject = GetOrAdd(invitationId);
        subject.OnNext(new(invitationId, OrganizationSetupAcceptanceStatus.Recorded, organizationName));
    }

    /// <summary>
    /// Marks an invitation as accepted. Called only once durable evidence - both the local record and the
    /// outbox - confirms setup (and any required legal fact) has actually been published, never from the
    /// command handling that accepted it: that would be a pre-append success signal, visible before the
    /// event even exists in the log, let alone the outbox.
    /// </summary>
    /// <param name="invitationId">The invitation identifier.</param>
    /// <param name="organizationName">The name of the organization that was set up.</param>
    public void MarkAccepted(InvitationId invitationId, TenantName organizationName)
    {
        var subject = GetOrAdd(invitationId);
        _acceptedAt[invitationId] = DateTimeOffset.UtcNow;
        subject.OnNext(new(invitationId, OrganizationSetupAcceptanceStatus.Accepted, organizationName));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _cleanupTimer.Dispose();
        foreach (var subscription in _subscriptions.Values)
        {
            subscription.OnCompleted();
            subscription.Dispose();
        }

        _subscriptions.Clear();
    }

    BehaviorSubject<OrganizationSetupAcceptanceStatusView> GetOrAdd(InvitationId invitationId) =>
        _subscriptions.GetOrAdd(invitationId, static key => new(new(key, OrganizationSetupAcceptanceStatus.Pending, TenantName.NotSet)));

    void Cleanup()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var (invitationId, acceptedAt) in _acceptedAt)
        {
            if (now - acceptedAt < _entryRetention)
            {
                continue;
            }

            if (_subscriptions.TryRemove(invitationId, out var subject))
            {
                subject.OnCompleted();
                subject.Dispose();
            }

            _acceptedAt.TryRemove(invitationId, out _);
        }
    }
}
