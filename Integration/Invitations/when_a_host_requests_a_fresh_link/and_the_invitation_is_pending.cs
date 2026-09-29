// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;

namespace Ante.Integration.Invitations.when_a_host_requests_a_fresh_link;

[Collection(ChronicleCollection.Name)]
public class and_the_invitation_is_pending : a_running_ante
{
    readonly Guid _invitationId = NewInvitationId();
    int _tokens;

    async Task Because()
    {
        await Invite(Host, _invitationId, JoinInvitation("Acme"));
        await Host.Publish(_invitationId, new InvitationReissueRequested());
        var tokens = await Eventually.Get(
            async () =>
            {
                var issued = (await Host.ReceivedFromAnte(_invitationId.ToString())).Where(entry => entry.Content is InvitationTokenIssued).ToList();
                return issued.Count >= 2 ? issued : null;
            },
            what: "a second token for the reissued invitation");
        _tokens = tokens.Count;
    }

    [Fact] void should_publish_a_fresh_token() => _tokens.ShouldEqual(2);
}
