// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;

namespace Ante.Integration.Invitations.when_an_owner_watches_acceptance;

/// <summary>
/// An open wizard's live status subscription must reach the terminal Accepted status without a reload, even
/// though the outbox-sourced read model usually trails the outbox append (Cratis/Ante#100).
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_the_invitation_is_accepted : a_running_ante
{
    const int Accepted = 2;

    readonly Guid _invitationId = NewInvitationId();
    readonly string _owner = $"owner-{Guid.NewGuid():N}";
    int _firstStatus;
    int _terminalStatus = -1;

    async Task Because()
    {
        var token = await Invite(Host, _invitationId, JoinInvitation());
        using var exchange = await Ante.ExchangeInvitation(token.Token, _owner);
        exchange.EnsureSuccessStatusCode();

        await using var stream = await Ante.WatchInvitationStatus(_invitationId, _owner);
        _firstStatus = StatusOf(await stream.Next());

        var result = await ExecuteOnceProjected(
            "/api/invitations/user-setup",
            new { invitationId = _invitationId, firstName = "Jane", lastName = "Doe", acceptedLegalTerms = false, acceptedLegalVersion = string.Empty },
            _owner);
        IsSuccess(result).ShouldBeTrue();
        await Host.WaitForFromAnte<InvitationToJoinTenantAccepted>(_invitationId.ToString());

        var deadline = DateTimeOffset.UtcNow + Eventually.DefaultTimeout;
        while (_terminalStatus != Accepted && DateTimeOffset.UtcNow < deadline)
        {
            _terminalStatus = StatusOf(await stream.Next(deadline - DateTimeOffset.UtcNow));
        }
    }

    [Fact] void should_start_as_pending() => _firstStatus.ShouldEqual(0);
    [Fact] void should_deliver_the_accepted_status_to_the_open_subscription() => _terminalStatus.ShouldEqual(Accepted);

    static int StatusOf(System.Text.Json.JsonDocument frame) => frame.RootElement.GetProperty("data").GetProperty("status").GetInt32();
}
