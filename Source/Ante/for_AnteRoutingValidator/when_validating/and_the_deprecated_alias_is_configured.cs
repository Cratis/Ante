// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.for_AnteRoutingValidator.when_validating;

public class and_the_deprecated_alias_is_configured : Specification
{
    AnteOptions _options = new() { InboxSourceStore = "SomeOtherHostStore" };

    void Because() => AnteRoutingValidator.Validate(_options);

    [Fact] void should_select_the_alias_as_the_single_host_store() =>
        Assert.Equal(["SomeOtherHostStore"], _options.HostStores);
}
#endif
