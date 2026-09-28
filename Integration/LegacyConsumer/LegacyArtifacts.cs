// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;

namespace Ante.Integration.LegacyConsumer;

/// <summary>The generation-2 host contract: the token has no PII annotation or generation-3 metadata.</summary>
/// <param name="FlowType">The invitation flow.</param>
/// <param name="Token">The signed capability.</param>
/// <param name="ExpiresAt">The signed expiry.</param>
[EventType("InvitationTokenIssued", generation: 2)]
public record LegacyInvitationTokenIssued(int FlowType, string Token, DateTimeOffset ExpiresAt);

/// <summary>The host's original join-invitation event shape.</summary>
/// <param name="Email">Recipient address.</param>
/// <param name="TenantName">Inviting organization.</param>
/// <param name="Roles">Invited roles.</param>
[EventType("UserInvitedToJoinTenant")]
public record LegacyUserInvitedToJoinTenant(string Email, string TenantName, IReadOnlyList<string> Roles);

/// <summary>Only the older token reader and its join invitation are registered.</summary>
public sealed class LegacyArtifacts : IClientArtifactsProvider
{
    public IEnumerable<Type> EventTypes => [typeof(LegacyInvitationTokenIssued), typeof(LegacyUserInvitedToJoinTenant)];
    public IEnumerable<Type> EventTypeMigrators => [];
    public IEnumerable<Type> ComplianceForTypesProviders => [];
    public IEnumerable<Type> ComplianceForPropertiesProviders => [];
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
