// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations;

/// <summary>Host-authored presentation copy for the two invitation journeys.</summary>
public class InvitationOptions
{
    /// <summary>Gets or sets the user invitation setup copy.</summary>
    public InvitationSetupOptions UserSetup { get; set; } = new();

    /// <summary>Gets or sets the organization invitation setup copy.</summary>
    public InvitationSetupOptions OrganizationSetup { get; set; } = new();
}
