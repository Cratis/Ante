// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Legal;

/// <summary>
/// Selects the in-process legal source or one trusted host outbox stream.
/// </summary>
public class LegalOptions
{
    /// <summary>
    /// Gets or sets the source mode: InProcess (default) or Inbox.
    /// </summary>
    public string Source { get; set; } = "InProcess";

    /// <summary>
    /// Gets or sets the trusted host store that publishes the set.
    /// </summary>
    public string? PublisherStore { get; set; }

    /// <summary>
    /// Gets or sets the event source id for the host's document set in its outbox.
    /// </summary>
    public string? DocumentSetId { get; set; }

    /// <summary>
    /// Validates the mode and its immutable routing at startup.
    /// </summary>
    /// <param name="options">The resolved routing settings.</param>
    /// <exception cref="LegalSourceConfigurationInvalid">The selected route is incomplete or unsafe.</exception>
    public static void Validate(AnteOptions options)
    {
        var legal = options.Legal;
        if (legal.Source == "InProcess" && legal.PublisherStore is null && legal.DocumentSetId is null)
        {
            return;
        }

        if (legal.Source != "Inbox" || string.IsNullOrWhiteSpace(legal.PublisherStore) ||
            string.IsNullOrWhiteSpace(legal.DocumentSetId) ||
            !options.HostStores!.Contains(legal.PublisherStore, StringComparer.Ordinal) ||
            Guid.TryParse(legal.DocumentSetId, out _))
        {
            throw new LegalSourceConfigurationInvalid();
        }
    }
}

/// <summary>
/// The exception that is thrown when the configured legal source is not a trusted, unambiguous route.
/// </summary>
public class LegalSourceConfigurationInvalid() : Exception(
    "Ante:Legal:Source must be InProcess or Inbox. Inbox requires Ante:Legal:PublisherStore in Ante:HostStores " +
    "and a nonempty, non-GUID Ante:Legal:DocumentSetId; these settings are not used in InProcess mode.");
