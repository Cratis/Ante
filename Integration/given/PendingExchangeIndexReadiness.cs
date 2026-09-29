// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Invitations.Accepting;

namespace Ante.Integration.given;

/// <summary>Holds the exchange write gate closed even while MongoDB is writable.</summary>
public sealed class PendingExchangeIndexReadiness : IExchangeIndexReadiness
{
    public bool IsReady => false;
}
