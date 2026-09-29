// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Invitations;
using Ante.Contracts.Organization;
using Ante.for_ReadModels.when_projecting_a_later_event_generation.given;
using Ante.Invitations.OrganizationSetup;
using Ante.Organization.Names;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;

namespace Ante.for_ReadModels.when_projecting_a_later_event_generation;

/// <summary>
/// The Chronicle kernel builds projections against the first-generation schema of every event type, so AutoMap
/// finds no schema for an event projected at a later generation and silently leaves every same-named property
/// unset (Cratis/Chronicle#4367). The in-process read-model scenario resolves schemas differently and does not
/// reproduce it: an <see cref="OrganizationNameClaim"/> built from a self-service registration kept no name, so a
/// host release failed on it and the name was never freed. This pins the precondition: such a property is mapped
/// explicitly, on read models and on the nested and child types inside them.
/// </summary>
public class and_a_property_shares_its_name_with_the_event : Specification
{
    Type[] _laterGenerationEvents = [];
    ProjectedShape[] _readModels = [];
    (string Shape, Type EventType)[] _laterGenerationSources = [];
    string[] _violations = [];
    Type[] _fluentProjections = [];
    string[] _plantedViolations = [];

    void Because()
    {
        var types = typeof(AnteOptions).Assembly.GetTypes().Concat(typeof(OrganizationRegistrationCompleted).Assembly.GetTypes()).ToArray();
        _laterGenerationEvents = [.. types.Where(type => Attribute.IsDefined(type, typeof(EventTypeAttribute)) && type.GetEventType().Generation.Value > 1)];
        _readModels = [.. typeof(AnteOptions).Assembly.GetTypes().Where(type => type.HasModelBoundProjectionAttributes()).Select(type => LaterGenerationAutoMaps.ShapeOf(type))];
        _laterGenerationSources =
        [
            .. _readModels.SelectMany(LaterGenerationAutoMaps.SourcesOf)
                .Where(source => _laterGenerationEvents.Contains(source.EventType))
                .Select(source => (source.Shape.Name, source.EventType)),
        ];
        _violations = [.. _readModels.SelectMany(LaterGenerationAutoMaps.ViolationsIn)];
        _fluentProjections =
        [
            .. typeof(AnteOptions).Assembly.GetTypes().Where(type => !type.IsAbstract && type.GetInterfaces()
                .Any(@interface => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IProjectionFor<>))),
        ];
        _plantedViolations = [.. Planted().SelectMany(LaterGenerationAutoMaps.ViolationsIn)];
    }

    [Fact]
    void should_find_the_later_generation_events() => Assert.Superset(
        new HashSet<Type> { typeof(OrganizationRegistrationCompleted), typeof(InvitationTokenIssued), typeof(InvitationRejected) },
        _laterGenerationEvents.ToHashSet());

    [Fact]
    void should_find_the_read_models_built_from_them() => Assert.Superset(
        new HashSet<(string, Type)>
        {
            (nameof(OrganizationNameClaim), typeof(OrganizationRegistrationCompleted)),
            (nameof(OrganizationSetupProgress), typeof(OrganizationRegistrationCompleted)),
            (nameof(OrganizationSetupPublished), typeof(OrganizationRegistrationCompleted)),
        },
        _laterGenerationSources.ToHashSet());

    [Fact] void should_map_every_such_property_explicitly() => Assert.Empty(_violations);

    // The fluent builder is not inspected here; a fluent projection needs this check extended before it lands.
    [Fact] void should_have_no_fluent_projection_left_unchecked() => Assert.Empty(_fluentProjections);

    [Fact]
    void should_detect_every_planted_auto_map() => Assert.Equal(
        ["Claim.TenantName from OrganizationRegistrationCompleted", "Owner.Tenants.TenantName from OrganizationRegistrationCompleted", "Owner.Setup.TenantName from OrganizationRegistrationCompleted"],
        _plantedViolations);

    static ProjectedShape[] Planted()
    {
        static ProjectedMember TenantName(params Attribute[] attributes) => new("TenantName", attributes);
        var child = new ProjectedShape("Owner.Tenants", [], [TenantName()]);
        var nested = new ProjectedShape("Owner.Setup", [new FromEventAttribute<OrganizationRegistrationCompleted>()], [TenantName()]);
        return
        [
            new("Claim", [new FromEventAttribute<OrganizationRegistrationCompleted>()], [TenantName()]),
            new("Mapped", [new FromEventAttribute<OrganizationRegistrationCompleted>()], [TenantName(new SetFromAttribute<OrganizationRegistrationCompleted>(nameof(OrganizationRegistrationCompleted.TenantName)))]),
            new("Opted", [new FromEventAttribute<OrganizationRegistrationCompleted>()], [TenantName(new NoAutoMapAttribute())]),
            new("Removed", [new RemovedWithAttribute<OrganizationRegistrationCompleted>()], [TenantName()]),
            new("FirstGeneration", [new FromEventAttribute<InvitationToCreateTenantAccepted>()], [TenantName()]),
            new(
                "Owner",
                [],
                [
                    new("Tenants", [new ChildrenFromAttribute<OrganizationRegistrationCompleted>()], child),
                    new("Setup", [new NestedAttribute()], nested),
                ]),
        ];
    }
}
#endif
