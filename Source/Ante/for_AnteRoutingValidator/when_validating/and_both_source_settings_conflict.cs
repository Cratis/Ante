// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Microsoft.Extensions.Configuration;

namespace Ante.for_AnteRoutingValidator.when_validating;

public class and_both_source_settings_conflict : Specification
{
    AnteOptions _options = null!;
    IConfiguration _configuration = null!;
    Exception? _result;

    void Establish()
    {
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ante:HostStores:0"] = "Studio",
            ["Ante:InboxSourceStore"] = "Direct",
        }).Build();
        _options = _configuration.GetSection("Ante").Get<AnteOptions>()!;
    }

    void Because() => _result = Cratis.Specifications.Catch.Exception(() => AnteRoutingValidator.Validate(_options, _configuration.GetSection("Ante")));

    [Fact] void should_fail_at_startup() => Assert.IsType<AnteHostStoresInvalid>(_result);
}
#endif
