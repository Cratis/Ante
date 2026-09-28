// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.for_AnteRoutingValidator.when_validating;

public class and_source_names_are_invalid : Specification
{
    [Theory]
    [InlineData("Ante", "Direct", "Direct")]
    [InlineData("Ante", "Ante", null)]
    [InlineData("Ante", "  ", null)]
    [InlineData("Ante", null, null)]
    void should_reject_duplicates_blank_entries_self_sources_and_empty_lists(string ownStore, string? first, string? second)
    {
        var stores = new List<string>();
        if (first is not null)
        {
            stores.Add(first);
        }
        if (second is not null)
        {
            stores.Add(second);
        }
        var options = new AnteOptions { EventStore = ownStore, HostStores = stores };
        Assert.Throws<AnteHostStoresInvalid>(() => AnteRoutingValidator.Validate(options));
    }
}
#endif
