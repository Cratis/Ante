// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupProgress.when_projecting;

public class and_an_invitation_was_accepted : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    ReadModelScenario<OrganizationSetupProgress> _scenario = null!;

    async Task Establish()
    {
        _scenario = new ReadModelScenario<OrganizationSetupProgress>();
        await _scenario.Given
            .ForEventSource(_invitationId)
            .Events(new InvitationToCreateTenantAccepted(
                "Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Owner"]));
    }

    [Fact] void should_have_the_organization_name() => Assert.Equal((TenantName)"Acme", _scenario.Instance!.OrganizationName);
    [Fact] void should_not_have_recorded_a_legal_fact() => Assert.False(_scenario.Instance!.LegalRecorded);
}
#endif
