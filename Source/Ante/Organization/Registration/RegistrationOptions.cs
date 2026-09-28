// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Organization.Registration;

/// <summary>
/// Configures self-service registration (<c language="csharp">/register</c>) for one Ante deployment: whether it is
/// offered at all, how much of it one sign-in or one client may consume, which host-supplied signup context
/// it carries through to the host, and the host-authored copy shown around the wizard.
/// </summary>
public class RegistrationOptions
{
    /// <summary>
    /// The largest number of context keys a deployment may allow.
    /// </summary>
    public const int MaximumContextKeys = 16;

    /// <summary>
    /// The longest context key or value Ante accepts, in characters.
    /// </summary>
    public const int MaximumContextLength = 200;

    /// <summary>
    /// Gets or sets a value indicating whether self-service registration is offered. When false, the
    /// lobby shows a neutral "sign-up is not available" page at <c language="csharp">/register</c> and both
    /// <c language="csharp">BeginRegistration</c> and <c language="csharp">RegisterOrganization</c> are rejected server-side.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets an optional URL the closed page links to (for example a waitlist or contact page).
    /// </summary>
    public string ClosedUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the most organizations one sign-in (subject and identity provider) may register within
    /// <see cref="Window"/>. Zero means unlimited.
    /// </summary>
    public int MaxPerIdentity { get; set; }

    /// <summary>
    /// Gets or sets the sliding window <see cref="MaxPerIdentity"/> applies to.
    /// </summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Gets or sets how many registration requests one client may make per minute. Zero disables the limit.
    /// </summary>
    public int RequestsPerMinutePerClient { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the client address is taken from the first
    /// <c language="csharp">X-Forwarded-For</c> entry. Enable only when Ante is reachable exclusively through a proxy
    /// that overwrites that header; otherwise the connection's remote address is used.
    /// </summary>
    public bool TrustForwardedFor { get; set; }

    /// <summary>
    /// Gets or sets the query-string keys the lobby keeps from the <c language="csharp">/register</c> link and publishes as
    /// signup context on <c language="csharp">OrganizationRegistrationCompleted</c>. Any other key is dropped.
    /// </summary>
    public IList<string> ContextKeys { get; set; } = [];

    /// <summary>
    /// Gets or sets the host-authored copy shown on the registration page, keyed by locale
    /// (for example <c language="csharp">en</c> or <c language="csharp">nb-NO</c>).
    /// </summary>
    public IDictionary<string, RegistrationContent> Content { get; set; } = new Dictionary<string, RegistrationContent>(StringComparer.OrdinalIgnoreCase);
}
