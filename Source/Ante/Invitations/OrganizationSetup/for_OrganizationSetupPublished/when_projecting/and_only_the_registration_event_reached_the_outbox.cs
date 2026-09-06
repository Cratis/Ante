// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupPublished.when_projecting;

public class and_only_the_registration_event_reached_the_outbox : Specification
{
    static readonly InvitationId _registrationId = InvitationId.New();

    ReadModelScenario<OrganizationSetupPublished> _scenario = null!;

    async Task Establish()
    {
        _scenario = new ReadModelScenario<OrganizationSetupPublished>();
        await _scenario.Given
            .ForEventSource(_registrationId)
            .Events(new OrganizationRegistrationCompleted(
                "Acme", "sub-1", "github", "Jane", MiddleName.NotSet, "Doe", "jane@example.com"));
    }

    [Fact] void should_exist() => Assert.NotNull(_scenario.Instance);
    [Fact] void should_have_published_the_registration() => Assert.True(_scenario.Instance!.AcceptancePublished);
    [Fact] void should_not_have_published_a_legal_fact() => Assert.False(_scenario.Instance!.LegalPublished);
}
#endif
