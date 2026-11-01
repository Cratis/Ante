// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Invitations.Receiving;

namespace Ante.Onboarding;

/// <summary>
/// Decides whether an invitation is allowed by the onboarding contract of the application.
/// </summary>
public static class OnboardingPolicy
{
    /// <summary>
    /// Gets the reason an invitation receipt is not allowed, or null when it is. Nothing is restricted unless the
    /// contract is enabled and lists invitation types or personas.
    /// </summary>
    /// <param name="onboarding">The onboarding contract.</param>
    /// <param name="receipt">The join-tenant or create-tenant receipt.</param>
    /// <returns>The rejection reason, or null.</returns>
    public static InvitationRejectionReason? Restriction(OnboardingOptions onboarding, object receipt)
    {
        if (!onboarding.Enabled)
        {
            return null;
        }

        var (type, roles) = receipt switch
        {
            JoinTenantInvitationReceived join => (OnboardingOptions.JoinTenant, join.Roles),
            CreateTenantInvitationReceived create => (OnboardingOptions.CreateTenant, create.Roles),
            _ => (string.Empty, []),
        };
        if (type.Length == 0)
        {
            return null;
        }

        if (onboarding.InvitationTypes is { Length: > 0 } types && !types.Contains(type, StringComparer.Ordinal))
        {
            return InvitationRejectionReason.InvitationTypeNotAllowed;
        }

        return onboarding.Personas is { Length: > 0 } personas &&
            roles.Any(role => !personas.Contains(role.Value, StringComparer.Ordinal))
                ? InvitationRejectionReason.PersonaNotGrantable
                : null;
    }
}
