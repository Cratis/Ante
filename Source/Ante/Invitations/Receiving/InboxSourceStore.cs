// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations.Receiving;

/// <summary>
/// The legacy Direct source name retained for the default configuration, historical receipt markers,
/// and the existing observer cursor. It no longer controls routing: Ante:HostStores does.
/// </summary>
public static class InboxSourceStore
{
    /// <summary>The source store used by existing Direct deployments.</summary>
    public const string Name = "Direct";
}
