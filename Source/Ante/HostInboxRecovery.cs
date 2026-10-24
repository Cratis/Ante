// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante;

/// <summary>
/// Tracks continuous host-inbox unavailability across readiness and liveness probes for this instance.
/// </summary>
/// <param name="timeProvider">The monotonic clock used to measure the outage.</param>
/// <param name="options">The configured recovery grace period.</param>
public class HostInboxRecovery(TimeProvider timeProvider, IOptions<HostInboxHealthOptions> options)
{
    readonly Lock _lock = new();
    long? _unhealthySince;

    /// <summary>
    /// Gets whether an observed outage has exceeded the recovery grace period.
    /// </summary>
    public bool HasExpired
    {
        get
        {
            lock (_lock)
            {
                return _unhealthySince is { } since && timeProvider.GetElapsedTime(since) > options.Value.HostInboxRecoveryGracePeriod;
            }
        }
    }

    /// <summary>
    /// Records a readiness observation, resetting the outage on recovery.
    /// </summary>
    /// <param name="ready">Whether every host inbox is ready.</param>
    public void Observe(bool ready)
    {
        lock (_lock)
        {
            _unhealthySince = ready ? null : _unhealthySince ?? timeProvider.GetTimestamp();
        }
    }
}
