// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using MongoDB.Driver;

namespace Ante.Organization.Names.for_ClaimedOrganizationNameSearch;

public class when_the_name_is_blank : Specification
{
    readonly IMongoCollection<OrganizationNameClaim> _claims = Substitute.For<IMongoCollection<OrganizationNameClaim>>();
    bool _result;

    async Task Because() => _result = await new ClaimedOrganizationNameSearch(_claims).Contains("  ");

    [Fact] void should_not_find_a_claimed_name() => _result.ShouldBeFalse();

    [Fact]
    async Task should_not_search_the_collection() =>
        await _claims.DidNotReceive().CountDocumentsAsync(Arg.Any<FilterDefinition<OrganizationNameClaim>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>());
}
#endif
