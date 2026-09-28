// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Organization.Registration;

/// <summary>
/// Host-authored copy for the registration page in one locale. Every value is plain text; links must be
/// absolute <c language="csharp">https:</c> (or same-origin relative) URLs and are otherwise ignored.
/// </summary>
public class RegistrationContent
{
    /// <summary>
    /// Gets or sets the heading shown above the wizard, replacing the default subtitle.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the introduction or offer text shown above the wizard.
    /// </summary>
    public string Intro { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets short highlight lines shown as a list (for example what the trial includes).
    /// </summary>
    public IList<string> Highlights { get; set; } = [];

    /// <summary>
    /// Gets or sets the URL of the host's pricing page.
    /// </summary>
    public string PricingUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL an existing user should use to log in instead.
    /// </summary>
    public string LoginUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message shown once registration has completed, while the lobby hands over to the host.
    /// </summary>
    public string CompletionMessage { get; set; } = string.Empty;
}
