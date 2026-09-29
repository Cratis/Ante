// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;

namespace Ante.Integration.Invitations.when_ante_restarts;

/// <summary>
/// An invitation a host publishes while Ante is down is picked up once Ante is back (Cratis/Ante#67).
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_an_invitation_arrives_while_it_is_down : a_running_ante
{
    readonly Guid _invitationId = NewInvitationId();
    InvitationTokenIssued _issued;

    async Task Because()
    {
        await Restart(whileStopped: () => Host.Publish(_invitationId, JoinInvitation("Acme"), subject: Guid.NewGuid()));
        _issued = await Host.WaitForFromAnte<InvitationTokenIssued>(_invitationId.ToString());
    }

    [Fact] void should_issue_the_token_after_the_restart() => _issued.Token.ShouldNotBeEmpty();
}
