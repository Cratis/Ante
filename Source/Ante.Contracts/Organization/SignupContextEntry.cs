// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Contracts.Organization;

/// <summary>
/// One key/value pair of signup context carried from the registration link.
/// </summary>
/// <param name="Key">The query-string key.</param>
/// <param name="Value">The query-string value.</param>
public record SignupContextEntry(string Key, string Value);
