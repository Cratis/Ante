// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.for_FixedNamespaceResolver.when_resolving;

/// <summary>
/// Ante is single-tenant per deployment, not request-selected multi-tenant - unlike
/// <see cref="ClaimsBasedNamespaceResolver"/>, nothing about the caller or the current identity should ever
/// change what this resolver returns.
/// </summary>
public class and_resolved_more_than_once : Specification
{
    static readonly EventStoreNamespaceName _configured = "Acme";

    FixedNamespaceResolver _resolver = null!;
    EventStoreNamespaceName _first = null!;
    EventStoreNamespaceName _second = null!;

    void Establish() => _resolver = new(_configured);

    void Because()
    {
        _first = _resolver.Resolve();
        _second = _resolver.Resolve();
    }

    [Fact] void should_resolve_to_the_same_namespace_every_time() => Assert.Equal(_first, _second);
}
#endif
