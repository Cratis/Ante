// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.UserSetup.for_UserSetupProgress.when_projecting;

public class and_only_the_acceptance_event_occurred : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    ReadModelScenario<UserSetupProgress> _scenario = null!;

    async Task Establish()
    {
        _scenario = new ReadModelScenario<UserSetupProgress>();
        await _scenario.Given
            .ForEventSource(_invitationId)
            .Events(new InvitationToJoinTenantAccepted(
                "Acme", "github", "sub-1", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", ["Member"]));
    }

    [Fact] void should_exist() => Assert.NotNull(_scenario.Instance);
    [Fact] void should_have_the_invitation_id() => Assert.Equal(_invitationId, _scenario.Instance!.Id);
    [Fact] void should_not_have_recorded_a_legal_fact() => Assert.False(_scenario.Instance!.LegalRecorded);
}
#endif
