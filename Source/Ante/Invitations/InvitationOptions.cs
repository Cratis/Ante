// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations;

/// <summary>Host-authored presentation copy for the two invitation journeys.</summary>
public class InvitationOptions
{
    /// <summary>
    /// Gets or sets how many invitation requests (stage, exchange, setup, status and pending reads) one client may make per
    /// minute. The budget is independent of <c language="csharp">Registration:RequestsPerMinutePerClient</c>. Zero disables the limit.
    /// </summary>
    public int RequestsPerMinutePerClient { get; set; }

    /// <summary>Gets or sets the user invitation setup copy.</summary>
    public InvitationSetupOptions UserSetup { get; set; } = new();

    /// <summary>Gets or sets the organization invitation setup copy.</summary>
    public InvitationSetupOptions OrganizationSetup { get; set; } = new();
}
