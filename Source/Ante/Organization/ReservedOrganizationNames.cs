// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Organization;

/// <summary>
/// Decides whether an organization name is one of the deployment's reserved names.
/// </summary>
public static class ReservedOrganizationNames
{
    /// <summary>
    /// Determines whether the given name is reserved by configuration.
    /// </summary>
    /// <param name="options">The deployment's options.</param>
    /// <param name="organizationName">The proposed organization name.</param>
    /// <returns>True when the name matches a reserved name, ignoring case and surrounding whitespace.</returns>
    public static bool IsReserved(AnteOptions options, string? organizationName) =>
        !string.IsNullOrWhiteSpace(organizationName) &&
        options.Organization.ReservedNames.Any(reserved =>
            string.Equals(reserved?.Trim(), organizationName.Trim(), StringComparison.OrdinalIgnoreCase));
}
