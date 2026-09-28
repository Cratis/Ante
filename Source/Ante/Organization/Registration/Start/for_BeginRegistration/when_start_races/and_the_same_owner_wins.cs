// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Organization.Registration.Start.for_BeginRegistration.given;
using Cratis.Arc.Validation;

namespace Ante.Organization.Registration.Start.for_BeginRegistration.when_start_races;

public class and_the_same_owner_wins : a_racing_start
{
    Result<ValidationResult, IEnumerable<object>> _result = null!;

    async Task Because() => _result = await new BeginRegistration(Id).Handle(Accessor, Resolver, Store, Microsoft.Extensions.Options.Options.Create(new AnteOptions()), TimeProvider.System);

    [Fact] void should_succeed_after_confirming_the_winning_owner() => _result.TryGetError(out _).ShouldBeTrue();
    [Fact]
    void should_not_append_another_start()
    {
        _result.TryGetError(out var events);
        events.ShouldBeEmpty();
    }
    [Fact] void should_recheck_the_authoritative_start_history() => HistoryReads.ShouldEqual(3);
}
#endif
