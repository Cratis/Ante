// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Chronicle.Projections.ModelBound;

namespace Ante.for_ReadModels.when_projecting_a_later_event_generation.given;

/// <summary>
/// A projected type - a read model, or a nested or child type inside one - described by the attributes Chronicle
/// builds its projection from. Kept apart from reflection so the check can be exercised on planted shapes: a real
/// type carrying these attributes would be discovered and registered as a projection.
/// </summary>
/// <param name="Name">The path of the projected type, for reporting.</param>
/// <param name="Attributes">The type-level attributes.</param>
/// <param name="Members">The constructor parameters and properties.</param>
public record ProjectedShape(string Name, IReadOnlyList<Attribute> Attributes, IReadOnlyList<ProjectedMember> Members);

/// <summary>
/// A member of a <see cref="ProjectedShape"/>.
/// </summary>
/// <param name="Name">The member name.</param>
/// <param name="Attributes">The member's attributes.</param>
/// <param name="Inner">The nested or child type the member projects into, when it has one.</param>
public record ProjectedMember(string Name, IReadOnlyList<Attribute> Attributes, ProjectedShape? Inner = null);

/// <summary>
/// Finds members Chronicle would have to AutoMap from an event projected at a generation above the first.
/// </summary>
public static class LaterGenerationAutoMaps
{
    /// <summary>
    /// Describes a model-bound read model and every nested or child type inside it.
    /// </summary>
    /// <param name="type">The read model type.</param>
    /// <param name="name">The path to report it under.</param>
    /// <returns>The shape.</returns>
    public static ProjectedShape ShapeOf(Type type, string? name = default)
    {
        name ??= type.Name;
        var members = type.GetConstructors().SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => (Name: parameter.Name!, Type: parameter.ParameterType, Attributes: parameter.GetCustomAttributes()))
            .Concat(type.GetProperties().Select(property => (property.Name, Type: property.PropertyType, Attributes: property.GetCustomAttributes())))
            .GroupBy(member => member.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                Attribute[] attributes = [.. group.SelectMany(member => member.Attributes)];
                var projectsInto = attributes.Any(attribute => attribute is IChildrenFromAttribute or INestedAttribute);
                return new ProjectedMember(group.Key, attributes, projectsInto ? ShapeOf(ElementTypeOf(group.First().Type), $"{name}.{group.Key}") : null);
            });
        return new(name, [.. type.GetCustomAttributes()], [.. members]);
    }

    /// <summary>
    /// Gets every event type a shape, or a type inside it, builds from together with where it is built.
    /// </summary>
    /// <param name="shape">The shape to search.</param>
    /// <returns>The projected shape and event type pairs.</returns>
    public static IEnumerable<(ProjectedShape Shape, Type EventType)> SourcesOf(ProjectedShape shape) =>
        shape.Attributes.Concat(shape.Members.SelectMany(member => member.Attributes.Where(attribute => attribute is not IChildrenFromAttribute)))
            .Where(IsMapping)
            .SelectMany(EventTypesOf)
            .Distinct()
            .Select(eventType => (shape, eventType))
            .Concat(shape.Members.Where(member => member.Inner is not null).SelectMany(member =>
                member.Attributes.Where(attribute => attribute is IChildrenFromAttribute).SelectMany(EventTypesOf).Select(eventType => (member.Inner!, eventType))
                    .Concat(SourcesOf(member.Inner!))));

    /// <summary>
    /// Gets the members AutoMap would fill from an event at a later generation.
    /// </summary>
    /// <param name="shape">The shape to check.</param>
    /// <returns>A description of each such member.</returns>
    public static IEnumerable<string> ViolationsIn(ProjectedShape shape) => SourcesOf(shape)
        .Where(source => source.EventType.GetEventType().Generation.Value > 1)
        .SelectMany(source => AutoMappedFrom(source.Shape, source.EventType).Select(member => $"{source.Shape.Name}.{member} from {source.EventType.Name}"))
        .Distinct();

    static IEnumerable<string> AutoMappedFrom(ProjectedShape shape, Type eventType)
    {
        var eventProperties = eventType.GetProperties().Select(property => property.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return shape.Members
            .Where(member => eventProperties.Contains(member.Name) &&
                !member.Attributes.Any(attribute => attribute is NoAutoMapAttribute || (IsMapping(attribute) && EventTypesOf(attribute).Contains(eventType))))
            .Select(member => member.Name);
    }

    // Removal and clearing name the event without reading its properties, so no AutoMap happens for them.
    static bool IsMapping(Attribute attribute) =>
        attribute is not (IRemovedWithAttribute or IRemovedWithJoinAttribute or IClearWithAttribute) && EventTypesOf(attribute).Any();

    static IEnumerable<Type> EventTypesOf(Attribute attribute) => attribute.GetType() is { IsGenericType: true } type
        ? type.GetGenericArguments().Where(argument => Attribute.IsDefined(argument, typeof(EventTypeAttribute)))
        : [];

    static Type ElementTypeOf(Type type)
    {
        if (type == typeof(string))
        {
            return type;
        }

        if (type.IsArray)
        {
            return type.GetElementType()!;
        }

        return type.GetInterfaces().Append(type)
            .FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))?
            .GetGenericArguments()[0] ?? type;
    }
}
#endif
