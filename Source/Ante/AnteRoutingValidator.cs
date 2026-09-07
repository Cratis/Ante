// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante;

/// <summary>
/// Validates that <see cref="AnteOptions"/> describes a routing configuration Ante can actually honor, so a
/// misconfigured deployment fails loudly at startup instead of silently misrouting events.
/// </summary>
public static class AnteRoutingValidator
{
    /// <summary>
    /// Validates the routing-relevant settings on <paramref name="options"/>.
    /// </summary>
    /// <param name="options">The <see cref="AnteOptions"/> to validate.</param>
    /// <exception cref="AnteEventStoreNotConfigured">Thrown when <see cref="AnteOptions.EventStore"/> is empty.</exception>
    /// <exception cref="AnteNamespaceNotConfigured">Thrown when <see cref="AnteOptions.Namespace"/> is empty.</exception>
    /// <exception cref="InboxSourceStoreCannotBeReconfigured">
    /// Thrown when <see cref="AnteOptions.InboxSourceStore"/> disagrees with the compiled
    /// <see cref="Invitations.Receiving.InboxSourceStore.Name"/> constant.
    /// </exception>
    public static void Validate(AnteOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.EventStore))
        {
            throw new AnteEventStoreNotConfigured();
        }

        if (string.IsNullOrWhiteSpace(options.Namespace))
        {
            throw new AnteNamespaceNotConfigured();
        }

        if (!string.Equals(options.InboxSourceStore, Invitations.Receiving.InboxSourceStore.Name, StringComparison.Ordinal))
        {
            throw new InboxSourceStoreCannotBeReconfigured(options.InboxSourceStore, Invitations.Receiving.InboxSourceStore.Name);
        }
    }
}

/// <summary>
/// The exception that is thrown when Ante is configured with an empty <c>Ante:EventStore</c>.
/// </summary>
public class AnteEventStoreNotConfigured() : Exception(
    "Ante:EventStore is empty. The Chronicle event store this instance runs against must be named " +
    "explicitly - leave the setting out of configuration entirely to accept the documented default " +
    "('Ante') rather than supplying an empty value for it.");

/// <summary>
/// The exception that is thrown when Ante is configured with an empty <c>Ante:Namespace</c>.
/// </summary>
public class AnteNamespaceNotConfigured() : Exception(
    "Ante:Namespace is empty. The fixed Chronicle namespace this instance runs against must be named " +
    "explicitly - leave the setting out of configuration entirely to accept the documented default " +
    "('Default') rather than supplying an empty value for it.");

/// <summary>
/// The exception that is thrown when <c>Ante:InboxSourceStore</c> disagrees with the compiled
/// <see cref="Invitations.Receiving.InboxSourceStore.Name"/> constant.
/// </summary>
/// <param name="configured">The value read from configuration.</param>
/// <param name="compiled">The compile-time constant this build was actually compiled with.</param>
public class InboxSourceStoreCannotBeReconfigured(string configured, string compiled) : Exception(
    $"Ante:InboxSourceStore is set to '{configured}', but this build of Ante cross-subscribes to the host " +
    $"store '{compiled}' (Source/Ante/Invitations/Receiving/InboxSourceStore.cs). Chronicle's [EventStore] " +
    "attribute - the only mechanism for pointing an observer at another store - requires a compile-time " +
    "constant argument, tracked upstream as Cratis/Chronicle#3951 as unsupported at runtime, so this " +
    $"setting cannot retarget the subscription. Remove the setting (or set it to '{compiled}') to accept " +
    "the compiled default, or change the InboxSourceStore.Name constant and rebuild.");
