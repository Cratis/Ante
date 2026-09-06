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
    /// Gets the status stream for an invitation, optionally seeded from durable setup progress.
    /// </summary>
    /// <remarks>
    /// Seeding keeps re-entry idempotent: a fresh in-memory entry starts out pending, so without the
    /// durable snapshot a user returning after a restart would see a stale pending state even though
    /// setup already completed.
    /// </remarks>
    /// <param name="invitationId">The invitation identifier.</param>
    /// <param name="durableProgress">The durable setup progress for the invitation, if any exists.</param>
    /// <returns>An observable status stream.</returns>
    public ISubject<OrganizationSetupAcceptanceStatusView> GetStatus(InvitationId invitationId, OrganizationSetupProgress? durableProgress = null)
    {
        var subject = GetOrAdd(invitationId);
        if (durableProgress is not null)
        {
            MarkAccepted(invitationId, durableProgress.OrganizationName);
        }

        return subject;
    }

    /// <summary>
    /// Marks an invitation as accepted. Ante's own job is done the moment the command handling that
    /// calls this returns - there is no external confirmation to wait for, so acceptance is immediate.
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
