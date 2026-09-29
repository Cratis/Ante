// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;

namespace Ante.Invitations.Issuing;

/// <summary>
/// Registers the invitation-token upgrade window.
/// </summary>
public static class InvitationTokenUpgradeWindowRegistration
{
    /// <summary>
    /// Registers <see cref="IInvitationTokenUpgradeWindow"/> as a singleton that stays closed until
    /// <see cref="DeferredInvitationTokenUpgradeWindow.Load"/> has read it from MongoDB.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="expiry">The configured token expiry, bounding the window.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <remarks>
    /// Resolving the window never touches MongoDB, so a temporarily unavailable database cannot stop the host from
    /// starting. <see cref="Accepting.AcceptedInvitationIndexRegistration"/> loads it, with retries, from a scope of
    /// its own (<see cref="IMongoCollection{TDocument}"/> is scoped: its database follows the current tenant).
    /// </remarks>
    public static IServiceCollection AddInvitationTokenUpgradeWindow(this IServiceCollection services, TimeSpan expiry)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(provider => new DeferredInvitationTokenUpgradeWindow(expiry, provider.GetRequiredService<TimeProvider>()));
        return services.AddSingleton<IInvitationTokenUpgradeWindow>(provider => provider.GetRequiredService<DeferredInvitationTokenUpgradeWindow>());
    }
}
