// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using MongoDB.Driver;

namespace Ante.Organization.Names.for_ClaimedOrganizationNameSearch;

public class when_the_name_is_claimed : Specification
{
    IMongoCollection<OrganizationNameClaim> _claims = null!;
    bool _result;

    void Establish()
    {
        _claims = Substitute.For<IMongoCollection<OrganizationNameClaim>>();
        _claims.CountDocumentsAsync(Arg.Any<FilterDefinition<OrganizationNameClaim>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(1L);
    }

    async Task Because() => _result = await new ClaimedOrganizationNameSearch(_claims).Contains("Acme");

    [Fact] void should_find_the_claimed_name() => _result.ShouldBeTrue();
}
#endif
