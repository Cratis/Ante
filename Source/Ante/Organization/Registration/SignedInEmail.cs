// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Identity;
using Microsoft.AspNetCore.Http;

namespace Ante.Organization.Registration;

/// <summary>
/// Reads the email address of whoever is signed in right now, out of the identity the authentication
/// proxy forwarded. Self-service registration has no other source for it - nobody types it, and there
/// is no invitation carrying it - so an address missed here is an organization nobody can be contacted
/// about.
/// </summary>
/// <remarks>
/// The proxy forwards the provider's claims verbatim, and providers disagree on which claim carries the
/// address: an OpenID Connect sign-in usually has <c language="csharp">email</c>, Microsoft Entra puts it on <c language="csharp">upn</c> or
/// <c language="csharp">preferred_username</c>, and the proxy additionally stamps the user details it was given onto the
/// name claim and the <c language="csharp">x-ms-client-principal-name</c> header. Every one of those is asked, most
/// specific first.
/// <para>
/// Each candidate has to look like an address before it is accepted, because the least specific of them
/// do not have to be one - a GitHub sign-in reports its login there. Recording that as the address the
/// organization signed up with would be worse than recording nothing: nothing is visibly absent, a
/// login is silently wrong.
/// </para>
/// </remarks>
public static class SignedInEmail
{
    // Asked in order, from the claims a provider issues specifically for the address down to the ones
    // that merely tend to hold it.
    static readonly string[] _claimTypes =
    [
        ClaimTypes.Email,
        "email",
        ClaimTypes.Upn,
        "upn",
        "preferred_username",
        ClaimTypes.Name
    ];

    /// <summary>
    /// Resolves the signed-in user's email address.
    /// </summary>
    /// <param name="user">The principal the authentication proxy's identity was rebuilt into.</param>
    /// <param name="headers">The request headers, which carry the forwarded user details.</param>
    /// <returns>The address, or an empty string when the identity carries none.</returns>
    public static string Resolve(ClaimsPrincipal? user, IHeaderDictionary? headers)
    {
        var candidates = _claimTypes
            .Select(claimType => user?.FindFirstValue(claimType))
            .Append(headers?[MicrosoftIdentityPlatformHeaders.IdentityNameHeader].FirstOrDefault());

        return candidates.FirstOrDefault(IsAddress) ?? string.Empty;
    }

    /// <summary>
    /// Decides whether a value read off the identity is an email address at all.
    /// </summary>
    /// <param name="value">The value to judge.</param>
    /// <returns><see langword="true"/> when the value is an address; otherwise <see langword="false"/>.</returns>
    public static bool IsAddress(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var at = value.IndexOf('@', StringComparison.Ordinal);

        return at > 0 && at == value.LastIndexOf('@') && at < value.Length - 1 && !value.Contains(' ', StringComparison.Ordinal);
    }
}
