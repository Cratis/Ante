// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;

namespace Ante.Organization.Registration.for_SignedInEmail.when_resolving;

public class and_the_identity_carries_an_email_claim : Specification
{
    string _result = string.Empty;

    void Because()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Email, "jane@example.com")]);
        _result = SignedInEmail.Resolve(new ClaimsPrincipal(identity), null);
    }

    [Fact] void should_resolve_the_address() => Assert.Equal("jane@example.com", _result);
}
#endif
