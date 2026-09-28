// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Organization;

/// <summary>
/// Organization-name rules for one Ante deployment.
/// </summary>
public class OrganizationOptions
{
    /// <summary>
    /// Gets or sets names that can never be claimed, compared case-insensitively - typically names that
    /// collide with the host's own routes or subdomains (for example <c language="csharp">admin</c>, <c language="csharp">app</c>, <c language="csharp">api</c>).
    /// </summary>
    public IList<string> ReservedNames { get; set; } = [];
}
