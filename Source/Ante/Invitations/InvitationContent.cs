// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations;

/// <summary>Allowlisted plain-text copy; it cannot replace legal or validation messages.</summary>
public class InvitationContent
{
    /// <summary>Gets or sets the subtitle below the deployment branding.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional plain-text introduction.</summary>
    public string Intro { get; set; } = string.Empty;

    /// <summary>Gets or sets the successful host-outcome completion message.</summary>
    public string CompletionMessage { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional absolute HTTPS support URL.</summary>
    public string SupportUrl { get; set; } = string.Empty;
}
