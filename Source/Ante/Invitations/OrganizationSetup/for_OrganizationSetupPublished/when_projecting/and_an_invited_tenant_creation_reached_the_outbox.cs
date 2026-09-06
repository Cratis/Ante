// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupPublished.when_projecting;

public class and_an_invited_tenant_creation_reached_the_outbox : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    ReadModelScenario<OrganizationSetupPublished> _scenario = null!;

    async Task Establish()
    {
        _scenario = new ReadModelScenario<OrganizationSetupPublished>();
        await _scenario.Given
            .ForEventSource(_invitationId)
            .Events(new InvitationToCreateTenantAccepted(
                "Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Owner"]));
    }

    [Fact] void should_exist() => Assert.NotNull(_scenario.Instance);
    [Fact] void should_have_published_the_acceptance() => Assert.True(_scenario.Instance!.AcceptancePublished);
}
#endif
