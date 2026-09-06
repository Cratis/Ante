// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations.UserSetup;

/// <summary>
/// Defines the backchannel Ante uses to ask a host whether an identity-provider subject is already
/// associated with one of its users.
/// </summary>
/// <remarks>
/// The subject is passed as a <see cref="string"/> for the same reason
/// <see cref="InvitationToJoinTenantAccepted.IdentityProviderSubject"/> is one - it is an opaque value
/// minted by an external identity provider, and the host owns the concept it maps onto.
/// </remarks>
public interface IIdentityBackchannel
{
    /// <summary>
    /// Determines whether an identity-provider subject already signs in as a user in an organization.
    /// </summary>
    /// <param name="organization">The organization the invited user is about to join.</param>
    /// <param name="identityProviderSubject">The subject the identity provider issued for the signed-in user.</param>
    /// <returns>True when a user in the organization is already associated with the subject.</returns>
    Task<bool> IsSubjectAlreadyAssociatedWithAUser(TenantName organization, string identityProviderSubject);
}

/// <summary>
/// The answer the host's identity backchannel gives about a subject.
/// </summary>
/// <param name="IsInUse">Whether a user in the organization is already associated with the subject.</param>
public record IdentityBackchannelAnswer(bool IsInUse);

/// <summary>
/// Asks the configured host backchannel over HTTP whether an identity-provider subject is already
/// taken.
/// </summary>
/// <remarks>
/// The check is a pre-flight that exists so the invited user gets a clear error instead of a silent
/// failure; the authoritative guard is the uniqueness constraint the host enforces when it appends the
/// association. An unconfigured or unreachable backchannel therefore lets onboarding continue rather
/// than blocking every invitation on a deployment detail or a transient outage.
/// </remarks>
/// <param name="httpClient">The HTTP client used to reach the host.</param>
/// <param name="options">The Ante options carrying the configured backchannel URL.</param>
/// <param name="logger">The logger.</param>
public class IdentityBackchannel(
    HttpClient httpClient,
    IOptions<AnteOptions> options,
    ILogger<IdentityBackchannel> logger) : IIdentityBackchannel
{
    /// <inheritdoc/>
    public async Task<bool> IsSubjectAlreadyAssociatedWithAUser(TenantName organization, string identityProviderSubject)
    {
        var backchannelUrl = options.Value.IdentityBackchannelUrl;

        if (string.IsNullOrWhiteSpace(backchannelUrl)
            || string.IsNullOrWhiteSpace(organization)
            || string.IsNullOrWhiteSpace(identityProviderSubject))
        {
            return false;
        }

        var url = $"{backchannelUrl.TrimEnd('/')}/in-use?organization={Uri.EscapeDataString(organization)}&subject={Uri.EscapeDataString(identityProviderSubject)}";

        try
        {
            var answer = await httpClient.GetFromJsonAsync<IdentityBackchannelAnswer>(url);
            return answer?.IsInUse == true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or NotSupportedException or System.Text.Json.JsonException)
        {
            logger.LogIdentityBackchannelUnavailable(organization, ex);
            return false;
        }
    }
}
