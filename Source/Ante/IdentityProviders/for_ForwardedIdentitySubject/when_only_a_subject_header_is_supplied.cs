// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Ante.IdentityProviders.for_ForwardedIdentitySubject;

public class when_only_a_subject_header_is_supplied : Specification
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("trusted", "victim")]
    public void should_require_an_authenticated_principal_before_reading_the_header(string? authenticationType, string? expected)
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([], authenticationType)) };
        context.Request.Headers["x-ms-client-principal-id"] = "victim";
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);
        Assert.Equal(expected, ForwardedIdentitySubject.Resolve(accessor));
    }
}
#endif
