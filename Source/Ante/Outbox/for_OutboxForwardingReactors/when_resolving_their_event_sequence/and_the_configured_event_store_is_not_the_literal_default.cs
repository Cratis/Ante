// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.OrganizationSetup;
using Ante.Invitations.UserSetup;
using Ante.Legal;
using Ante.Organization.Registration;

namespace Ante.Outbox.for_OutboxForwardingReactors.when_resolving_their_event_sequence;

/// <summary>
/// The events these four reactors handle (<c>InvitationToCreateTenantAccepted</c>,
/// <c>InvitationToJoinTenantAccepted</c>, <c>OrganizationRegistrationCompleted</c>, <c>LegalTermsAccepted</c>)
/// are all declared in <c>Cratis.Ante.Contracts</c>, whose assembly-level <c>[EventStore("Ante")]</c>
/// attribute exists so a *host's own* reactor can observe them without knowing Ante's configured store
/// name. Chronicle resolves an unattributed reactor's event sequence by comparing that declared store
/// ("Ante", the compiled literal) against the reactor's *actual* current store - so before these reactors
/// were pinned with <c>[EventLog]</c>, a deployment renamed away from the literal "Ante" (which is exactly
/// what <see cref="AnteOptions.EventStore"/> exists to let an operator do, and what
/// Documentation/configuration.md's own "DirectLobby" example demonstrates) made every one of these
/// reactors resolve to a nonexistent inbox sequence instead of the local event log - forwarding to the
/// outbox would then silently never happen. This spec pins that regression down directly against
/// Chronicle's own resolution rule, independent of any particular configured store name.
/// </summary>
public class and_the_configured_event_store_is_not_the_literal_default : Specification
{
    const string _renamedStore = "DirectLobby";

    EventSequenceId _organizationSetup = null!;
    EventSequenceId _joinTenantAcceptance = null!;
    EventSequenceId _organizationRegistration = null!;
    EventSequenceId _legalTermsAcceptance = null!;

    void Because()
    {
        // Called via the fully-qualified extension class - Cratis.Chronicle.Reducers.ReducerTypeExtensions
        // declares an overload with an identical signature, which makes the ordinary extension-method
        // call syntax ambiguous even though this file never references reducers.
        _organizationSetup = Cratis.Chronicle.Reactors.ReactorTypeExtensions.GetEventSequenceId(typeof(OrganizationSetupOutbox), _renamedStore);
        _joinTenantAcceptance = Cratis.Chronicle.Reactors.ReactorTypeExtensions.GetEventSequenceId(typeof(JoinTenantAcceptanceOutbox), _renamedStore);
        _organizationRegistration = Cratis.Chronicle.Reactors.ReactorTypeExtensions.GetEventSequenceId(typeof(OrganizationRegistrationOutbox), _renamedStore);
        _legalTermsAcceptance = Cratis.Chronicle.Reactors.ReactorTypeExtensions.GetEventSequenceId(typeof(LegalTermsAcceptanceOutbox), _renamedStore);
    }

    [Fact]
    void should_route_organization_setup_forwarding_to_the_local_log() =>
        Assert.Equal(EventSequenceId.Log, _organizationSetup);

    [Fact]
    void should_route_join_tenant_acceptance_forwarding_to_the_local_log() =>
        Assert.Equal(EventSequenceId.Log, _joinTenantAcceptance);

    [Fact]
    void should_route_organization_registration_forwarding_to_the_local_log() =>
        Assert.Equal(EventSequenceId.Log, _organizationRegistration);

    [Fact]
    void should_route_legal_terms_acceptance_forwarding_to_the_local_log() =>
        Assert.Equal(EventSequenceId.Log, _legalTermsAcceptance);
}
#endif
