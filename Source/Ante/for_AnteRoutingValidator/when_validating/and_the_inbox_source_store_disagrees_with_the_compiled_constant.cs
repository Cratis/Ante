// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving;

namespace Ante.for_AnteRoutingValidator.when_validating;

/// <summary>
/// Chronicle's <c>[EventStore]</c> attribute requires a compile-time constant argument (Cratis/Chronicle#3951),
/// so <see cref="AnteOptions.InboxSourceStore"/> cannot actually retarget
/// <see cref="InboxSourceStore.Name"/> at runtime. A deployment supplying a different value here must be
/// rejected loudly rather than silently ignored - the option would otherwise look accepted while doing
/// nothing.
/// </summary>
public class and_the_inbox_source_store_disagrees_with_the_compiled_constant : Specification
{
    AnteOptions _options = null!;
    Exception _result = null!;

    void Establish() => _options = new() { InboxSourceStore = "SomeOtherHostStore" };

    void Because() => _result = Cratis.Specifications.Catch.Exception(() => AnteRoutingValidator.Validate(_options));

    [Fact] void should_fail_loudly() => Assert.IsType<InboxSourceStoreCannotBeReconfigured>(_result);

    [Fact]
    void should_name_both_the_configured_and_compiled_values()
    {
        Assert.Contains("SomeOtherHostStore", _result.Message);
        Assert.Contains(InboxSourceStore.Name, _result.Message);
    }
}
#endif
