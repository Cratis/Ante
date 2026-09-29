// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Invitations.Accepting;

namespace Ante.Integration.given;

/// <summary>Holds the attested exchange gate closed without taking writable MongoDB offline.</summary>
public sealed class MutableExchangeIndexReadiness : IExchangeIndexReadiness
{
    volatile bool _ready = true;

    public bool IsReady
    {
        get => _ready;
        set => _ready = value;
    }
}
