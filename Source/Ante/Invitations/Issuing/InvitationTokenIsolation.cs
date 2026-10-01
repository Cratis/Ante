// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations.Issuing;

/// <summary>
/// Gives every deployment its own token issuer and audience, so tokens issued by one deployment are never
/// accepted by another - even when two deployments share a signing key.
/// </summary>
public static class InvitationTokenIsolation
{
    /// <summary>
    /// Gets the issuer a deployment derives when none is configured. Each component is percent-encoded so
    /// the colon separator is unambiguous and two deployments can never derive the same issuer.
    /// </summary>
    /// <param name="options">The deployment's options.</param>
    /// <returns>The derived issuer.</returns>
    public static string DerivedIssuer(AnteOptions options) =>
        $"urn:cratis:ante:{Uri.EscapeDataString(options.EventStore)}:{Uri.EscapeDataString(options.Namespace)}";

    /// <summary>
    /// Gets the audience a deployment derives when none is configured.
    /// </summary>
    /// <param name="options">The deployment's options.</param>
    /// <returns>The derived audience.</returns>
    public static string DerivedAudience(AnteOptions options) => $"{DerivedIssuer(options)}:lobby";

    /// <summary>
    /// Fills in the issuer and audience a deployment derives for any that is not configured, remembering
    /// beforehand whether either was configured (see <see cref="InvitationTokenConfig.IssuerOrAudienceConfigured"/>).
    /// </summary>
    /// <param name="config">The token configuration.</param>
    /// <param name="options">The deployment's options.</param>
    public static void ApplyDefaults(InvitationTokenConfig config, AnteOptions options)
    {
        config.RememberConfiguredIssuerAndAudience();

        if (string.IsNullOrWhiteSpace(config.Issuer))
        {
            config.Issuer = DerivedIssuer(options);
        }

        if (string.IsNullOrWhiteSpace(config.Audience))
        {
            config.Audience = DerivedAudience(options);
        }
    }
}
