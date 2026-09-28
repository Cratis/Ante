// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.OrganizationSetup;
using Ante.Invitations.UserSetup;
using Ante.Legal;
using Ante.Organization.Registration;
using Cratis.Chronicle.Projections.ModelBound;

namespace Ante.Outbox.for_OutboxForwardingReactors.when_discovering_model_bound_projections;

/// <summary>
/// Chronicle's client discovers any type carrying an <c language="csharp">EventSequenceAttribute</c> (such as
/// <c language="csharp">[EventLog]</c>) as a model-bound projection as well, and a projection registered under a
/// reactor's id takes over its observer with no event types - the reactor then never runs. The forwarding
/// reactors must pin the event log without being mistaken for projections.
/// </summary>
public class and_the_reactors_are_pinned_to_the_event_log : Specification
{
    static readonly Type[] _forwardingReactors =
    [
        typeof(OrganizationSetupOutbox),
        typeof(JoinTenantAcceptanceOutbox),
        typeof(OrganizationRegistrationOutbox),
        typeof(LegalTermsAcceptanceOutbox),
    ];

    Type[] _discoveredAsProjections = [];

    void Because() => _discoveredAsProjections = [.. _forwardingReactors.Where(type => type.HasModelBoundProjectionAttributes())];

    [Fact] void should_not_discover_any_of_them_as_a_model_bound_projection() => Assert.Empty(_discoveredAsProjections);
}
#endif
