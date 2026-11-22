// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Ante.Organization.Names;

/// <summary>
/// Searches claimed organization names for advisory command validation.
/// </summary>
/// <remarks>
/// Names are matched case-insensitively through <see cref="ClaimedOrganizationNames"/>.
/// See <see cref="ClaimedOrganizationNames.SearchIsAdvisory"/>: this cross-claim search only attributes a
/// friendly message to the organization name field; UniqueOrganizationNameConstraint enforces uniqueness at append time.
/// </remarks>
public interface IClaimedOrganizationNameSearch
{
    /// <summary>
    /// Determines whether an organization name is already claimed.
    /// </summary>
    /// <param name="organizationName">The organization name to look for.</param>
    /// <returns>True when the name is already claimed; otherwise false.</returns>
    Task<bool> Contains(string organizationName);
}

/// <summary>
/// Performs the existing advisory search without exposing its MongoDB collection as a command dependency.
/// </summary>
/// <param name="claims">The claimed organization names to search.</param>
// Arc#3942: direct MongoDB command parameters reject advisory annotations at runtime.
public class ClaimedOrganizationNameSearch(IMongoCollection<OrganizationNameClaim> claims) : IClaimedOrganizationNameSearch
{
    /// <inheritdoc/>
    public Task<bool> Contains(string organizationName) => ClaimedOrganizationNames.Contains(claims, organizationName);
}
