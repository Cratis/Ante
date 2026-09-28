// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Resources;

namespace Ante.Resources;

/// <summary>
/// Request-localized onboarding messages, with invariant English as the fallback.
/// </summary>
public static class Messages
{
    static readonly ResourceManager _resources = new("Ante.Resources.Messages", typeof(Messages).Assembly);

    /// <summary>
    /// Reads a localized message at validation/execution time, never during validator construction.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The text in the request's UI culture, falling back to English.</returns>
    public static string Get(string key) => _resources.GetString(key, CultureInfo.CurrentUICulture) ??
        _resources.GetString(key, CultureInfo.GetCultureInfo("en-US")) ?? key;

    /// <summary>
    /// Formats a localized message without changing parsing or numeric semantics.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="arguments">Substitution values.</param>
    /// <returns>The formatted message.</returns>
    public static string Format(string key, params object[] arguments) => string.Format(CultureInfo.InvariantCulture, Get(key), arguments);
}
