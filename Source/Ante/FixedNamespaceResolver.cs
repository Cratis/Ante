// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante;

/// <summary>
/// Resolves Ante's Chronicle namespace to the single, fixed value this deployment is configured with.
/// </summary>
/// <remarks>
/// Ante is single-tenant per deployment, not request-selected multi-tenant (see
/// <c>Documentation/boundaries.md#multi-tenancy-of-ante-itself</c>): a product needing several isolated
/// lobbies runs several Ante instances, each differentiated by <see cref="AnteOptions.EventStore"/>, not by
/// one instance resolving a different namespace per request. This resolver reflects that on purpose - it
/// always returns the same value regardless of the caller or the current identity, unlike
/// <see cref="ClaimsBasedNamespaceResolver"/>, which is Chronicle's building block for genuinely
/// multi-tenant clients.
/// </remarks>
/// <param name="namespaceName">The fixed <see cref="EventStoreNamespaceName"/> to always resolve to.</param>
public class FixedNamespaceResolver(EventStoreNamespaceName namespaceName) : IEventStoreNamespaceResolver
{
    /// <inheritdoc/>
    public EventStoreNamespaceName Resolve() => namespaceName;
}
