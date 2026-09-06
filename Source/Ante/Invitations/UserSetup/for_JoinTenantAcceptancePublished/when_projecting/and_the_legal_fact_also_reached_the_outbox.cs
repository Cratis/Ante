// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;

namespace Ante.Invitations.UserSetup.for_JoinTenantAcceptancePublished.when_projecting;

public class and_the_legal_fact_also_reached_the_outbox : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    ReadModelScenario<JoinTenantAcceptancePublished> _scenario = null!;

    async Task Establish()
    {
        _scenario = new ReadModelScenario<JoinTenantAcceptancePublished>();
        await _scenario.Given
            .ForEventSource(_invitationId)
            .Events(
                new InvitationToJoinTenantAccepted(
                    "Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Member"]),
                new LegalTermsAccepted("Acme", "github", "sub-1", "v1"));
    }

    [Fact] void should_have_published_the_legal_fact() => Assert.True(_scenario.Instance!.LegalPublished);
}
#endif
