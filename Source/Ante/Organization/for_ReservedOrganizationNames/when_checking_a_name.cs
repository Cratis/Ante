// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Organization.for_ReservedOrganizationNames;

public class when_checking_a_name : Specification
{
    static readonly AnteOptions _options = new() { Organization = new() { ReservedNames = ["admin", " App "] } };

    [Theory]
    [InlineData("admin", true)]
    [InlineData("ADMIN", true)]
    [InlineData("app", true)]
    [InlineData("acme", false)]
    [InlineData("", false)]
    void should_match_reserved_names_ignoring_case_and_whitespace(string name, bool reserved) =>
        Assert.Equal(reserved, ReservedOrganizationNames.IsReserved(_options, name));
}
#endif
