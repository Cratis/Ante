// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante;

/// <summary>
/// Configures the time allowed for host inbox recovery before liveness requests a restart.
/// </summary>
public class HostInboxHealthOptions
{
    /// <summary>
    /// Gets or sets the continuous unhealthy period tolerated by liveness. Must be positive and at most one day.
    /// </summary>
    public TimeSpan HostInboxRecoveryGracePeriod { get; set; } = TimeSpan.FromMinutes(3);
}
