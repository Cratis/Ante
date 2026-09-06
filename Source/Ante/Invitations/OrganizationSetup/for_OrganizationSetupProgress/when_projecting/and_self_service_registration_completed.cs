// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupProgress.when_projecting;

/// <summary>
/// Self-service registration shares this exact read model with invited tenant creation - the same
/// durable "Recorded" boundary must cover both flows so their status can be served the same way.
/// </summary>
public class and_self_service_registration_completed : Specification
{
    static readonly InvitationId _registrationId = InvitationId.New();

    ReadModelScenario<OrganizationSetupProgress> _scenario = null!;

    async Task Establish()
    {
        _scenario = new ReadModelScenario<OrganizationSetupProgress>();
        await _scenario.Given
            .ForEventSource(_registrationId)
            .Events(new OrganizationRegistrationCompleted(
                "Acme", "sub-1", "github", "Jane", MiddleName.NotSet, "Doe", "jane@example.com"));
    }

    [Fact] void should_have_the_organization_name() => Assert.Equal((TenantName)"Acme", _scenario.Instance!.OrganizationName);
    [Fact] void should_not_have_recorded_a_legal_fact() => Assert.False(_scenario.Instance!.LegalRecorded);
}
#endif
