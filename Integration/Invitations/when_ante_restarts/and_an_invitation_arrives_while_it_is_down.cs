// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Ante.Integration.given;

namespace Ante.Integration.Invitations.when_ante_restarts;

/// <summary>
/// An invitation a host publishes while Ante is down is picked up once Ante is back, and a link issued before the
/// restart still works without being reissued (Cratis/Ante#67, Cratis/Ante#112).
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_an_invitation_arrives_while_it_is_down : a_running_ante
{
    readonly Guid _invitationId = NewInvitationId();
    readonly Guid _earlierInvitationId = NewInvitationId();
    InvitationTokenIssued _earlier;
    InvitationTokenIssued _issued;
    HttpStatusCode _earlierExchange;
    int _earlierTokens;

    async Task Establish() => _earlier = await Invite(Host, _earlierInvitationId, JoinInvitation("Acme"));

    async Task Because()
    {
        await Restart(whileStopped: () => Host.Publish(_invitationId, JoinInvitation("Acme"), subject: Guid.NewGuid()));
        _issued = await Host.WaitForFromAnte<InvitationTokenIssued>(_invitationId.ToString());
        using var exchange = await Ante.ExchangeInvitation(_earlier.Token, $"user-{Guid.NewGuid():N}");
        _earlierExchange = exchange.StatusCode;
        _earlierTokens = (await Host.ReceivedFromAnte(_earlierInvitationId.ToString())).Count(entry => entry.Content is InvitationTokenIssued);
    }

    [Fact] void should_issue_the_token_after_the_restart() => _issued.Token.ShouldNotBeEmpty();
    [Fact] void should_accept_a_link_issued_before_the_restart() => _earlierExchange.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_not_reissue_the_earlier_token() => _earlierTokens.ShouldEqual(1);
}
