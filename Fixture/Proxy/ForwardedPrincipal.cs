// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Ante.Fixture.Proxy;

public static class ForwardedPrincipal
{
    // Matches the attested identity shape in Integration/given/AnteApplication.cs.
    public static void Apply(HttpRequest request, SignedInIdentity identity)
    {
        var principal = new
        {
            identityProvider = identity.ProviderKey,
            userId = identity.Subject,
            userDetails = identity.Email,
            userRoles = new[] { "authenticated" },
            claims = new[]
            {
                new { typ = "urn:cratis:identity:subject", val = identity.Subject },
                new { typ = "urn:cratis:identity:provider-key", val = identity.ProviderKey },
                new { typ = "urn:cratis:identity:issuer", val = identity.Issuer },
            },
        };
        request.Headers["x-ms-client-principal-id"] = identity.Subject;
        request.Headers["x-ms-client-principal-name"] = identity.Email;
        request.Headers["x-ms-client-principal"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(principal)));
    }
}
