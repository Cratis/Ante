// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations;

/// <summary>Presentation-only content for one invitation setup journey.</summary>
public class InvitationSetupOptions
{
    /// <summary>Gets or sets copy keyed by locale, resolved as exact locale, neutral language, en, then en-US.</summary>
    public IDictionary<string, InvitationContent> Content { get; set; } = new Dictionary<string, InvitationContent>(StringComparer.OrdinalIgnoreCase);
}
