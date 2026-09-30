// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ante;

/// <summary>
/// Gives Chronicle's <see cref="IEventSerializer"/> the per-scope lifetime it is designed for.
/// </summary>
/// <remarks>
/// <para>
/// The serializer holds the <see cref="IEventTypes"/> of the event store a scope resolves, so it must be scoped.
/// Chronicle registers it that way with <c language="csharp">TryAddScoped</c>, but <see cref="EventSerializer"/> still carries the
/// historical <c language="csharp">[Singleton]</c> attribute, and Arc's convention binding runs before Chronicle is added - so
/// the convention's singleton is registered first and Chronicle's scoped registration is skipped. The singleton
/// then depends on the scoped <see cref="IEventTypes"/>: Development's scope validation rejects it on first use,
/// and elsewhere it silently takes <see cref="IEventTypes"/> from the root scope for the life of the process.
/// </para>
/// <para>
/// Replacing the registrations restores the intended lifetime whichever registered first. A scope created for one
/// reactor delivery resolves the event store Ante is configured for (<see cref="AnteOptions.EventStore"/> and its
/// fixed namespace, see <see cref="FixedNamespaceResolver"/>) and gets the serializer of exactly that store.
/// </para>
/// </remarks>
public static class EventSerializerRegistration
{
    /// <summary>
    /// Registers <see cref="EventSerializer"/> and <see cref="IEventSerializer"/> as scoped, replacing any other lifetime.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to register with.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddScopedEventSerializer(this IServiceCollection services)
    {
        services.RemoveAll<IEventSerializer>();
        services.RemoveAll<EventSerializer>();
        services.AddScoped<EventSerializer>();
        services.AddScoped<IEventSerializer>(provider => provider.GetRequiredService<EventSerializer>());
        return services;
    }
}
