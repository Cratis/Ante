// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;
using Ante.Invitations.OrganizationSetup;
using Ante.Organization.Names;
using Ante.Organization.Registration;

namespace Ante.Integration.Organization.for_UniqueOrganizationNameConstraint.given;

public sealed class legacy_claim_artifacts : IClientArtifactsProvider
{
    readonly HostArtifacts _contracts = new();

    public IEnumerable<Type> EventTypes => _contracts.EventTypes.Concat([
        typeof(OrganizationNameReservationReceived),
        typeof(OrganizationNameReleaseReceived),
        typeof(OrganizationSetupCompleted),
        typeof(OrganizationRegistrationRecorded)]);

    public IEnumerable<Type> EventTypeMigrators => _contracts.EventTypeMigrators;
    public IEnumerable<Type> ComplianceForTypesProviders => _contracts.ComplianceForTypesProviders;
    public IEnumerable<Type> ComplianceForPropertiesProviders => _contracts.ComplianceForPropertiesProviders;
    public IEnumerable<Type> Projections => [];
    public IEnumerable<Type> ModelBoundProjections => [];
    public IEnumerable<Type> Reactors => [];
    public IEnumerable<Type> ReadModelReactors => [];
    public IEnumerable<Type> Reducers => [];
    public IEnumerable<Type> ReactorMiddlewares => [];
    public IEnumerable<Type> AdditionalEventInformationProviders => [];
    public IEnumerable<Type> ConstraintTypes => [];
    public IEnumerable<Type> UniqueConstraints => [];
    public IEnumerable<Type> UniqueEventTypeConstraints => [];
    public IEnumerable<Type> RemoveConstraintEventTypes => [];
    public IEnumerable<Type> EventSeeders => [];
}
