// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Microsoft.Extensions.Configuration;

namespace Ante.for_AnteRoutingValidator.when_validating;

public class and_multiple_source_stores_are_configured : Specification
{
    AnteOptions _options = null!;
    IConfiguration _configuration = null!;

    void Establish()
    {
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ante:EventStore"] = "StudioLobby",
            ["Ante:HostStores:0"] = "Studio",
            ["Ante:HostStores:1"] = "StudioAdmin",
        }).Build();
        _options = _configuration.GetSection("Ante").Get<AnteOptions>()!;
    }

    void Because() => AnteRoutingValidator.Validate(_options, _configuration.GetSection("Ante"));

    [Fact] void should_bind_both_sources_in_order() => Assert.Equal(["Studio", "StudioAdmin"], _options.HostStores);
}
#endif
