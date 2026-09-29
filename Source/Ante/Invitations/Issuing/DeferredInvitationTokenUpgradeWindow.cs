// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Ante.Invitations.Issuing;

/// <summary>
/// The upgrade window before and after it has been read from MongoDB. Until <see cref="Load"/> succeeds it accepts
/// no token without issuer and audience - it fails closed - so the host can start without the database.
/// </summary>
/// <param name="expiry">The configured token expiry, bounding the window.</param>
/// <param name="timeProvider">The clock recorded as the activation when this is the first run with isolation.</param>
public sealed class DeferredInvitationTokenUpgradeWindow(TimeSpan expiry, TimeProvider timeProvider) : IInvitationTokenUpgradeWindow
{
    volatile InvitationTokenUpgradeWindow _window = InvitationTokenUpgradeWindow.Closed;
    volatile bool _isLoaded;

    /// <summary>Gets a value indicating whether the window has been read from MongoDB.</summary>
    public bool IsLoaded => _isLoaded;

    /// <summary>
    /// Reads the window from MongoDB, recording its activation when this is the first run with isolation.
    /// </summary>
    /// <param name="database">Ante's MongoDB database.</param>
    /// <param name="cancellationToken">Cancels a pending connection on shutdown.</param>
    /// <returns>A task that completes once the window is loaded; it faults when MongoDB cannot be reached.</returns>
    public async Task Load(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        _window = await InvitationTokenUpgradeWindow.Load(database, timeProvider.GetUtcNow(), expiry, cancellationToken);
        _isLoaded = true;
    }

    /// <inheritdoc/>
    public bool AcceptsLegacyToken(DateTimeOffset issuedAt, DateTimeOffset expiresAt, DateTimeOffset now) =>
        _window.AcceptsLegacyToken(issuedAt, expiresAt, now);
}
