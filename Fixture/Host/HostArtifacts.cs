// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Invitations;
using Cratis.Chronicle;

namespace Ante.Fixture.Host;

// Copied from Integration/given/HostArtifacts.cs: register contracts, not Ante's own projections or reactors.
public sealed class HostArtifacts : IClientArtifactsProvider
{
    static readonly System.Reflection.Assembly _contracts = typeof(UserInvitedToJoinTenant).Assembly;
    readonly DefaultClientArtifactsProvider _default = DefaultClientArtifactsProvider.Default;

    public IEnumerable<Type> EventTypes => _default.EventTypes.Where(type => type.Assembly == _contracts);

    public IEnumerable<Type> EventTypeMigrators => _default.EventTypeMigrators.Where(type => type.Assembly == _contracts);

    public IEnumerable<Type> ComplianceForTypesProviders => _default.ComplianceForTypesProviders;

    public IEnumerable<Type> ComplianceForPropertiesProviders => _default.ComplianceForPropertiesProviders;

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
