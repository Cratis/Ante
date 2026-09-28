// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Ante.Invitations.OrganizationSetup;
using Ante.Invitations.UserSetup;
using Cratis.Chronicle.Projections.ModelBound;

namespace Ante.for_ReadModels.when_resolving_their_event_sequence;

/// <summary>
/// The events in <c language="csharp">Cratis.Ante.Contracts</c> carry an assembly-level <c language="csharp">[EventStore("Ante")]</c>
/// so a host can observe them from its inbox without knowing Ante's configured store name. When a model-bound read
/// model has no <see cref="EventSequenceAttribute"/>, Chronicle's client infers its sequence from that attribute: the
/// local event log only while the running store is literally named "Ante", otherwise <c language="csharp">inbox-Ante</c>.
/// Any deployment renamed through <see cref="AnteOptions.EventStore"/> then has read models that never see Ante's own
/// events - registration ownership never projected and every registrant stayed pending. Chronicle's resolver is
/// internal to its client, so this pins the precondition it honors: an explicit sequence on every such read model.
/// </summary>
public class and_they_project_events_declared_for_the_contracts_store : Specification
{
    Type[] _readModelsProjectingContractEvents = [];
    Type[] _unpinned = [];

    void Because()
    {
        _readModelsProjectingContractEvents =
        [
            .. typeof(AnteOptions).Assembly.GetTypes()
                .Where(type => type.HasModelBoundProjectionAttributes() &&
                    EventTypesProjectedBy(type).Any(eventType => eventType.GetEventStoreName() is not null)),
        ];
        _unpinned = [.. _readModelsProjectingContractEvents.Where(type => !Attribute.IsDefined(type, typeof(EventSequenceAttribute)))];
    }

    [Fact]
    void should_find_the_local_setup_read_models() => Assert.Superset(
        new HashSet<Type> { typeof(OrganizationSetupProgress), typeof(AcceptedOrganizationName), typeof(UserSetupProgress) },
        _readModelsProjectingContractEvents.ToHashSet());

    [Fact] void should_pin_every_one_of_them_to_an_explicit_event_sequence() => Assert.Empty(_unpinned);

    [Fact]
    void should_read_organization_setup_progress_from_the_local_event_log() =>
        Assert.Equal(EventSequenceId.Log, typeof(OrganizationSetupProgress).GetCustomAttribute<EventSequenceAttribute>()?.Sequence);

    [Fact]
    void should_read_accepted_organization_names_from_the_local_event_log() =>
        Assert.Equal(EventSequenceId.Log, typeof(AcceptedOrganizationName).GetCustomAttribute<EventSequenceAttribute>()?.Sequence);

    [Fact]
    void should_read_user_setup_progress_from_the_local_event_log() =>
        Assert.Equal(EventSequenceId.Log, typeof(UserSetupProgress).GetCustomAttribute<EventSequenceAttribute>()?.Sequence);

    // Chronicle infers from the events a projection builds from; removal events do not take part in that choice.
    static IEnumerable<Type> EventTypesProjectedBy(Type readModel)
    {
        var attributes = readModel.GetCustomAttributes()
            .Concat(readModel.GetProperties().SelectMany(property => property.GetCustomAttributes()))
            .Concat(readModel.GetConstructors().SelectMany(constructor => constructor.GetParameters()).SelectMany(parameter => parameter.GetCustomAttributes()));

        return attributes
            .Select(attribute => attribute.GetType())
            .Where(attributeType => attributeType.IsGenericType && !attributeType.Name.StartsWith("RemovedWith", StringComparison.Ordinal))
            .SelectMany(attributeType => attributeType.GetGenericArguments())
            .Where(argument => Attribute.IsDefined(argument, typeof(EventTypeAttribute)))
            .Distinct();
    }
}
#endif
