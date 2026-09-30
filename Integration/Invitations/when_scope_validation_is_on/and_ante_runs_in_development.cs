// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;

namespace Ante.Integration.Invitations.when_scope_validation_is_on;

/// <summary>
/// Development turns the default service provider's scope validation on. An invitation must still reach Ante's incoming
/// reactor and come back as a token: a scoped service resolved through a singleton fails every delivery there and nowhere
/// else (Cratis/Ante#142).
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_ante_runs_in_development : a_running_ante
{
    Guid _invitationId;
    InvitationTokenIssued _issued;

    protected override string EnvironmentName => "Development";

    async Task Because()
    {
        _invitationId = NewInvitationId();
        await Host.Publish(_invitationId, JoinInvitation(), subject: Guid.NewGuid());
        _issued = await Host.WaitForFromAnte<InvitationTokenIssued>(_invitationId.ToString());
    }

    [Fact] void should_deliver_a_token_back_to_the_host_inbox() => _issued.ShouldNotBeNull();
    [Fact] void should_issue_it_for_the_join_flow() => _issued.FlowType.ShouldEqual(InvitationFlowType.JoinTenant);
}
