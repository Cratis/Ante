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
    /// Registers <see cref="IInvitationTokenUpgradeWindow"/> as a singleton loaded, on first resolution, from a scope of its own.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="expiry">The configured token expiry, bounding the window.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <remarks>
    /// <see cref="IMongoCollection{TDocument}"/> is scoped (its database follows the current tenant), so it is never
    /// resolved from the root provider: Development's scope validation rejects that.
    /// </remarks>
    public static IServiceCollection AddInvitationTokenUpgradeWindow(this IServiceCollection services, TimeSpan expiry)
    {
        services.TryAddSingleton(TimeProvider.System);
        return services.AddSingleton<IInvitationTokenUpgradeWindow>(root =>
        {
            using var startupScope = root.CreateScope();
            var database = startupScope.ServiceProvider.GetRequiredService<IMongoCollection<Accepting.AcceptedInvitation>>().Database;
            return InvitationTokenUpgradeWindow.Load(database, root.GetRequiredService<TimeProvider>().GetUtcNow(), expiry).GetAwaiter().GetResult();
        });
    }
}
