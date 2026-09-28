// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Integration.given;

/// <summary>
/// The artifacts a minimal host registers: only Ante's contract event types (and their migrations).
/// </summary>
/// <remarks>
/// The default provider scans every loaded assembly, which in this process includes Ante itself - a host client
/// using it would register Ante's reactors and projections into the host store. Compliance metadata providers are
/// kept, so the contract's <c>[PII]</c> values are encrypted exactly as a real host's would be.
/// </remarks>
public sealed class HostArtifacts : IClientArtifactsProvider
{
    static readonly System.Reflection.Assembly _contracts = typeof(UserInvitedToJoinTenant).Assembly;
    readonly IClientArtifactsProvider _default = DefaultClientArtifactsProvider.Default;

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
