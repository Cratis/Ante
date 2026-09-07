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
    /// Gets or sets the fixed Chronicle namespace Ante runs against within <see cref="EventStore"/>.
    /// Ante is single-tenant per deployment, not request-selected multi-tenant - this value applies to
    /// every request this instance serves. Defaults to Chronicle's own default namespace, which is what
    /// an unconfigured instance already ran in before this setting existed.
    /// </summary>
    public string Namespace { get; set; } = EventStoreNamespaceName.Default.Value;

    /// <summary>
    /// Gets or sets the name of the host product's event store Ante's inbox reactor expects to
    /// cross-subscribe to. This does not retarget the subscription - Chronicle's <c>[EventStore]</c>
    /// attribute requires a compile-time constant, so the actual source store is
    /// <see cref="Invitations.Receiving.InboxSourceStore.Name"/> and changing it requires editing that
    /// constant and rebuilding (tracked upstream as Cratis/Chronicle#3951). This setting exists solely so
    /// a deployment that supplies a different value here fails loudly at startup via
    /// <see cref="AnteRoutingValidator"/> instead of the option being silently accepted and ignored.
    /// </summary>
    public string InboxSourceStore { get; set; } = Invitations.Receiving.InboxSourceStore.Name;

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

    /// <summary>
    /// Gets or sets the base URL of an optional host endpoint Ante calls to look up what happened to a
    /// specific onboarding attempt after Ante's own publication - for example, whether downstream
    /// provisioning in the host succeeded or failed. Purely informational: it is never required, never
    /// implies host membership or provisioning is Ante's responsibility, and never changes whether Ante's
    /// own onboarding published successfully. When empty, no host outcome is looked up or displayed and
    /// every wizard behaves exactly as it does without this setting - see
    /// <see href="https://github.com/Cratis/Ante/issues/22">Cratis/Ante#22</see>.
    /// </summary>
    public string HostOutcomeUrl { get; set; } = string.Empty;
}
