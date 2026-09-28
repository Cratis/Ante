// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Organization.Registration.when_registering.given;

namespace Ante.Organization.Registration.when_registering;

public class and_the_organization_name_is_reserved : a_started_registration
{
    void Establish() => Options.Organization.ReservedNames = ["admin"];

    Task Because() => Register("Admin");

    [Fact] void should_reject_the_registration() => Result.ShouldNotBeSuccessful();
    [Fact] void should_attribute_it_to_the_organization_name() =>
        Assert.Contains(Result.ValidationResults, result => result.Members.Any(member => member.Equals("organizationName", StringComparison.OrdinalIgnoreCase)));
}
#endif
