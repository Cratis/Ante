// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.for_FixedNamespaceResolver.when_resolving;

public class and_a_namespace_is_configured : Specification
{
    static readonly EventStoreNamespaceName _configured = "Acme";

    FixedNamespaceResolver _resolver = null!;
    EventStoreNamespaceName _result = null!;

    void Establish() => _resolver = new(_configured);

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_resolve_to_the_configured_namespace() => Assert.Equal(_configured, _result);
}
#endif
