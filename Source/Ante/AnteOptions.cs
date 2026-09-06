// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante;

/// <summary>
/// Represents the runtime configuration options for the Ante service, bound from the <c>Ante</c>
/// configuration section.
/// </summary>
public class AnteOptions
{
    /// <summary>
    /// Gets or sets the name of the Chronicle event store Ante runs against. A host embedding more than
    /// one Ante instance (for example one lobby per product) gives each its own store name here -
    /// there is no literal event store name anywhere in code.
    /// </summary>
    public string EventStore { get; set; } = "Ante";

    /// <summary>
    /// Gets or sets the base URL of the host application. Once an invitation is accepted or a
    /// self-service registration completes, the wizard redirects here.
    /// For localhost development use the pattern <c>http://{tenant}.localhost:8090/</c> where the
    /// placeholder <c>{tenant}</c> is replaced by the subdomain at runtime.
    /// </summary>
    public string HostAppUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL of the logo to display in the lobby.
    /// When empty, a generic Ante wordmark is used.
    /// This value can be overridden by mounting a file into the container.
    /// </summary>
    public string LogoUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL of a custom CSS file to inject into the lobby.
    /// When empty, the default lobby styles are used.
    /// This value can be overridden by mounting a file into the container.
    /// </summary>
    public string CustomCssUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base URL of the identity backchannel Ante calls to check whether the identity a
    /// user signed in with is already associated with a user in the organization they are joining
    /// (for example <c>https://host.example.com/api/internal/identity-providers</c>).
    /// When empty, the check is skipped and onboarding relies solely on the host's own uniqueness
    /// constraint.
    /// </summary>
    public string IdentityBackchannelUrl { get; set; } = string.Empty;
}
