// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupProgress.when_projecting;

public class and_legal_terms_were_also_accepted : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    ReadModelScenario<OrganizationSetupProgress> _scenario = null!;

    async Task Establish()
    {
        _scenario = new ReadModelScenario<OrganizationSetupProgress>();
        await _scenario.Given
            .ForEventSource(_invitationId)
            .Events(
                new InvitationToCreateTenantAccepted(
                    "Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Owner"]),
                new LegalTermsAccepted("Acme", "github", "sub-1", "v1"));
    }

    [Fact] void should_have_recorded_the_legal_fact() => Assert.True(_scenario.Instance!.LegalRecorded);
}
#endif
