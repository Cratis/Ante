// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Invitations.Receiving;
using Microsoft.Extensions.Configuration;

namespace Ante;

/// <summary>
/// Validates and resolves the destination store, namespace, and trusted host source stores at startup.
/// </summary>
public static class AnteRoutingValidator
{
    /// <summary>
    /// Validates and normalizes the routing settings. The configuration section distinguishes an omitted
    /// legacy key from an explicitly supplied value, including an empty value.
    /// </summary>
    /// <param name="options">The bound options to validate and normalize.</param>
    /// <param name="configuration">The Ante configuration section, when available.</param>
    /// <exception cref="AnteEventStoreNotConfigured">The local store is blank.</exception>
    /// <exception cref="AnteNamespaceNotConfigured">The namespace is blank.</exception>
    /// <exception cref="AnteHostStoresInvalid">A source is invalid or the old and new settings conflict.</exception>
    public static void Validate(AnteOptions options, IConfiguration? configuration = null)
    {
        if (string.IsNullOrWhiteSpace(options.EventStore))
        {
            throw new AnteEventStoreNotConfigured();
        }

        if (string.IsNullOrWhiteSpace(options.Namespace))
        {
            throw new AnteNamespaceNotConfigured();
        }

        ValidateAudience(options.Audience, nameof(AnteOptions.Audience));
        ValidateAudience(options.OperatorAudience, nameof(AnteOptions.OperatorAudience));

        var legacyPresent = options.InboxSourceStore is not null;
        var listPresent = options.HostStores is not null;
        if (configuration is not null)
        {
            legacyPresent = configuration.GetSection(nameof(AnteOptions.InboxSourceStore)).Exists();
            listPresent = configuration.GetSection(nameof(AnteOptions.HostStores)).Exists();
        }

        var stores = options.HostStores;
        if (!listPresent)
        {
            stores = legacyPresent ? [options.InboxSourceStore!] : [InboxSourceStore.Name];
        }

        if (legacyPresent && listPresent && (stores?.Count != 1 ||
            !string.Equals(stores[0], options.InboxSourceStore, StringComparison.Ordinal)))
        {
            throw new AnteHostStoresInvalid("Ante:HostStores conflicts with the deprecated Ante:InboxSourceStore setting.");
        }

        if (stores is null || stores.Count == 0 || stores.Count(name => !string.IsNullOrWhiteSpace(name)) != stores.Count)
        {
            throw new AnteHostStoresInvalid("Ante:HostStores must contain at least one nonempty host store.");
        }

        if (stores.Count != stores.Distinct(StringComparer.Ordinal).Count())
        {
            throw new AnteHostStoresInvalid("Ante:HostStores must not contain duplicate store names.");
        }

        if (stores.Contains(options.EventStore, StringComparer.Ordinal))
        {
            throw new AnteHostStoresInvalid("Ante:HostStores must not include Ante:EventStore.");
        }

        options.HostStores = [.. stores];
    }

    static void ValidateAudience(string? value, string setting)
    {
        // Empty means unset (an empty environment variable is how a deployment clears it); whitespace is a mistake.
        if (!string.IsNullOrEmpty(value) && (string.IsNullOrWhiteSpace(value) || value.Trim() != value))
        {
            throw new AnteAudienceInvalid($"Ante:{setting} must be a nonempty audience name without surrounding whitespace; leave it out or empty to disable audience routing.");
        }
    }
}

/// <summary>The exception thrown when Ante:EventStore is empty.</summary>
public class AnteEventStoreNotConfigured() : Exception(
    "Ante:EventStore is empty. The Chronicle event store this instance runs against must be named " +
    "explicitly - leave the setting out of configuration entirely to accept the documented default " +
    "('Ante') rather than supplying an empty value for it.");

/// <summary>The exception thrown when Ante:Namespace is empty.</summary>
public class AnteNamespaceNotConfigured() : Exception(
    "Ante:Namespace is empty. The fixed Chronicle namespace this instance runs against must be named " +
    "explicitly - leave the setting out of configuration entirely to accept the documented default " +
    "('Default') rather than supplying an empty value for it.");

/// <summary>The exception thrown when the host source store configuration is invalid.</summary>
/// <param name="message">A diagnostic identifying the invalid setting.</param>
public class AnteHostStoresInvalid(string message) : Exception(message);

/// <summary>
/// Thrown when <c language="csharp">Ante:Audience</c> or <c language="csharp">Ante:OperatorAudience</c> is not a usable audience name.
/// </summary>
/// <param name="message">The message.</param>
public class AnteAudienceInvalid(string message) : Exception(message);
