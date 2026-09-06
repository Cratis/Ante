// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;

namespace Ante.Organization.Registration.for_SignedInEmail.when_resolving;

public class and_the_identity_names_a_login_rather_than_an_address : Specification
{
    string _result = string.Empty;

    void Because()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "janedoe")]);
        _result = SignedInEmail.Resolve(new ClaimsPrincipal(identity), null);
    }

    [Fact]
    void should_resolve_to_an_empty_string_rather_than_a_login_handle() => Assert.Equal(string.Empty, _result);
}
#endif
