// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Ante.Organization.Registration.for_SignedInEmail.when_resolving;

public class and_only_a_forwarded_name_is_supplied : Specification
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("trusted", "victim@example.com")]
    public void should_read_the_header_only_for_an_authenticated_principal(string? authenticationType, string expected)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([], authenticationType));
        var headers = new HeaderDictionary { ["x-ms-client-principal-name"] = "victim@example.com" };
        SignedInEmail.Resolve(user, headers).ShouldEqual(expected);
    }
}
#endif
