// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.for_AnteRoutingValidator.when_validating;

public class and_the_event_store_is_empty : Specification
{
    AnteOptions _options = null!;
    Exception _result = null!;

    void Establish() => _options = new() { EventStore = " " };

    void Because() => _result = Cratis.Specifications.Catch.Exception(() => AnteRoutingValidator.Validate(_options));

    [Fact] void should_fail_loudly() => Assert.IsType<AnteEventStoreNotConfigured>(_result);
}
#endif
