// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Ante.Integration.given;

namespace Ante.Integration.Invitations.when_chronicle_restarts;

/// <summary>
/// An invitee watching their acceptance status keeps the same subscription across a kernel restart and sees the
/// acceptance they submit after it (Cratis/Ante#112).
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_an_owner_is_watching_acceptance : a_running_ante
{
    readonly Guid _invitationId = NewInvitationId();
    readonly string _owner = $"owner-{Guid.NewGuid():N}";
    int _firstStatus;
    JsonDocument _result;
    InvitationToJoinTenantAccepted _accepted;
    int _terminalStatus = -1;

    async Task Because()
    {
        var issued = await Invite(Host, _invitationId, JoinInvitation());
        using var exchange = await Ante.ExchangeInvitation(issued.Token, _owner);
        exchange.EnsureSuccessStatusCode();
        await using var stream = await Ante.WatchInvitationStatus(_invitationId, _owner);
        _firstStatus = Status(await stream.Next());

        await RestartChronicle();

        _result = await ExecuteOnceProjected(
            "/api/invitations/user-setup",
            new { invitationId = _invitationId, firstName = "Jane", lastName = "Doe", acceptedLegalTerms = false, acceptedLegalVersion = "" },
            _owner);
        _accepted = await Host.WaitForFromAnte<InvitationToJoinTenantAccepted>(_invitationId.ToString());
        var deadline = DateTimeOffset.UtcNow + Eventually.DefaultTimeout;
        while (_terminalStatus != 2 && DateTimeOffset.UtcNow < deadline)
        {
            _terminalStatus = Status(await stream.Next(deadline - DateTimeOffset.UtcNow));
        }
    }

    static int Status(JsonDocument frame) => frame.RootElement.GetProperty("data").GetProperty("status").GetInt32();

    [Fact] void should_start_as_pending() => _firstStatus.ShouldEqual(0);
    [Fact] void should_accept_the_onboarding_after_the_restart() => IsSuccess(_result).ShouldBeTrue();
    [Fact] void should_publish_the_acceptance() => _accepted.IdentityProviderSubject.ShouldEqual(_owner);
    [Fact] void should_deliver_terminal_status_to_the_existing_subscription() => _terminalStatus.ShouldEqual(2);
}
