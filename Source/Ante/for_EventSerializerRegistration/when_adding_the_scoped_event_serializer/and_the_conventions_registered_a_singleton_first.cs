// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;

namespace Ante.for_EventSerializerRegistration.when_adding_the_scoped_event_serializer;

public class and_the_conventions_registered_a_singleton_first : Specification
{
    ServiceProvider _provider = null!;
    IEventSerializer? _first;
    IEventSerializer? _sameScope;
    IEventSerializer? _otherScope;
    Exception? _error;
    ServiceDescriptor[] _registrations = [];

    void Because()
    {
        var services = new ServiceCollection();

        // Like Development: the event store's types are scoped, and a singleton cannot take them.
        services.AddScoped(_ => Substitute.For<IEventTypes>());
        services.AddSingleton(_ => Substitute.For<IClientArtifactsProvider>());
        services.AddSingleton(_ => Substitute.For<IClientArtifactsActivator>());
        services.AddSingleton(_ => new JsonSerializerOptions());

        // What Arc's convention binding registers for the [Singleton]-marked serializer.
        services.AddSingleton<IEventSerializer, EventSerializer>();
        services.AddSingleton<EventSerializer>();

        services.AddScopedEventSerializer();
        _registrations = services
            .Where(descriptor => descriptor.ServiceType == typeof(EventSerializer) || descriptor.ServiceType == typeof(IEventSerializer))
            .ToArray();

        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        try
        {
            using var scope = _provider.CreateScope();
            _first = scope.ServiceProvider.GetRequiredService<IEventSerializer>();
            _sameScope = scope.ServiceProvider.GetRequiredService<IEventSerializer>();
            using var other = _provider.CreateScope();
            _otherScope = other.ServiceProvider.GetRequiredService<IEventSerializer>();
        }
        catch (Exception error)
        {
            _error = error;
        }
    }

    [Fact] void should_not_fail_scope_validation() => _error.ShouldBeNull();
    [Fact] void should_register_each_service_once() => _registrations.Length.ShouldEqual(2);
    [Fact] void should_register_them_scoped() => _registrations.All(descriptor => descriptor.Lifetime == ServiceLifetime.Scoped).ShouldBeTrue();
    [Fact] void should_share_one_serializer_within_a_scope() => _sameScope.ShouldEqual(_first);
    [Fact] void should_give_the_next_scope_its_own_serializer() => _otherScope.ShouldNotEqual(_first);
}
#endif
