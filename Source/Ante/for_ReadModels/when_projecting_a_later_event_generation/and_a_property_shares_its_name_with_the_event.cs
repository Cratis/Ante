// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Ante.Contracts.Organization;
using Ante.Organization.Names;
using Cratis.Chronicle.Projections.ModelBound;

namespace Ante.for_ReadModels.when_projecting_a_later_event_generation;

/// <summary>
/// The Chronicle kernel builds projections against the first-generation schema of every event type, so AutoMap
/// finds no schema for an event projected at a later generation and silently leaves every same-named property
/// unset. The in-process read-model scenario resolves schemas differently and does not reproduce it: an
/// <see cref="OrganizationNameClaim"/> built from a self-service registration kept no name, so a host release
/// failed on it and the name was never freed. This pins the precondition: such a property is mapped explicitly.
/// </summary>
public class and_a_property_shares_its_name_with_the_event : Specification
{
    string[] _autoMappedFromLaterGenerations = [];

    void Because() => _autoMappedFromLaterGenerations =
    [
        .. typeof(AnteOptions).Assembly.GetTypes()
            .Where(type => type.HasModelBoundProjectionAttributes())
            .SelectMany(readModel => LaterGenerationEventsProjectedBy(readModel)
                .SelectMany(eventType => PropertiesAutoMappedFrom(readModel, eventType)
                    .Select(property => $"{readModel.Name}.{property} from {eventType.Name}"))),
    ];

    [Fact] void should_map_every_such_property_explicitly() => Assert.Empty(_autoMappedFromLaterGenerations);

    [Fact]
    void should_map_the_claimed_organization_name_explicitly() => Assert.Contains(
        typeof(OrganizationNameClaim).GetConstructors().SelectMany(constructor => constructor.GetParameters())
            .Single(parameter => parameter.Name == nameof(OrganizationNameClaim.TenantName))
            .GetCustomAttributes(),
        attribute => attribute is SetFromAttribute<OrganizationRegistrationCompleted>);

    static IEnumerable<Type> LaterGenerationEventsProjectedBy(Type readModel) => readModel.GetCustomAttributes()
        .Select(attribute => attribute.GetType())
        .Where(attributeType => attributeType.IsGenericType && attributeType.GetGenericTypeDefinition() == typeof(FromEventAttribute<>))
        .Select(attributeType => attributeType.GetGenericArguments()[0])
        .Where(eventType => eventType.GetEventType().Generation.Value > 1);

    static IEnumerable<string> PropertiesAutoMappedFrom(Type readModel, Type eventType)
    {
        var eventProperties = eventType.GetProperties().Select(property => property.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var members = readModel.GetConstructors().SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => (Name: parameter.Name!, Attributes: parameter.GetCustomAttributes()))
            .Concat(readModel.GetProperties().Select(property => (property.Name, Attributes: property.GetCustomAttributes())));

        return members
            .GroupBy(member => member.Name, StringComparer.OrdinalIgnoreCase)
            .Where(member => eventProperties.Contains(member.Key) &&
                !member.SelectMany(_ => _.Attributes).Any(attribute => IsMappingFrom(attribute, eventType)))
            .Select(member => member.Key);
    }

    static bool IsMappingFrom(Attribute attribute, Type eventType) =>
        attribute.GetType() is { IsGenericType: true } attributeType && attributeType.GetGenericArguments().Contains(eventType);
}
#endif
