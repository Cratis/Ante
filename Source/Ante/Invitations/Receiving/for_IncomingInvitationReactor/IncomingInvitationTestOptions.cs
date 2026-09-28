// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.Receiving;

internal static class IncomingInvitationTestOptions
{
    internal static IOptions<InvitationExchangeConfig> Legacy { get; } = Options.Create(new InvitationExchangeConfig());
}
#endif
