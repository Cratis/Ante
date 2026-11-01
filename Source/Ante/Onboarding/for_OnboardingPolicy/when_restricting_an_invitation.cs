// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Receiving;

namespace Ante.Onboarding.for_OnboardingPolicy;

public class when_restricting_an_invitation : Specification
{
    static readonly JoinTenantInvitationReceived _join = new("a@example.com", "Acme", ["Member"]);
    static readonly CreateTenantInvitationReceived _create = new("a@example.com", ["Owner", "Admin"]);

    [Fact]
    void should_allow_anything_when_the_contract_is_off() =>
        Assert.Null(OnboardingPolicy.Restriction(new() { Personas = ["Owner"] }, _join));

    [Fact]
    void should_allow_anything_when_no_personas_are_listed() =>
        Assert.Null(OnboardingPolicy.Restriction(new() { Enabled = true }, _join));

    [Fact]
    void should_allow_declared_personas() =>
        Assert.Null(OnboardingPolicy.Restriction(new() { Enabled = true, Personas = ["Member"] }, _join));

    [Fact]
    void should_reject_a_persona_outside_the_list() =>
        Assert.Equal(InvitationRejectionReason.PersonaNotGrantable, OnboardingPolicy.Restriction(new() { Enabled = true, Personas = ["Owner"] }, _create));

    [Fact]
    void should_reject_a_type_that_is_not_allowed() =>
        Assert.Equal(InvitationRejectionReason.InvitationTypeNotAllowed, OnboardingPolicy.Restriction(new() { Enabled = true, InvitationTypes = ["JoinTenant"] }, _create));
}
#endif
