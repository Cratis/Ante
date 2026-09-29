// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Ante.Integration.given;

namespace Ante.Integration.Invitations.when_two_ante_instances_share_a_store;

/// <summary>
/// The owner accepts through one instance and then opens the status on the other - a reconnect a load balancer sends
/// to another replica. The other instance never heard the in-memory notification, so its subscription is seeded from
/// the durable facts in the shared store and starts at the terminal status (Cratis/Ante#108, Cratis/Ante#119).
/// </summary>
/// <remarks>
/// A subscription that was already open on the other instance before the acceptance is only moved forward by
/// notifications raised on its own instance (<c>IPublicationStatusNotifier</c>), and which instance forwards an
/// acceptance is Chronicle's choice. Such a subscription reaches the terminal status when it is opened again, which is
/// what this spec asserts; it does not assert that an open subscription on the other instance advances by itself.
/// </remarks>
[Collection(ChronicleCollection.Name)]
public class and_the_owner_watches_on_the_other_instance : two_running_antes
{
    const int Accepted = 2;

    readonly Guid _invitationId = NewInvitationId();
    readonly string _owner = $"owner-{Guid.NewGuid():N}";
    int _firstStreamedStatus;
    int _snapshotStatus;
    int _strangerStatus;

    async Task Because()
    {
        var issued = await Invite(Host, _invitationId, JoinInvitation());
        using var exchange = await Ante.ExchangeInvitation(issued.Token, _owner);
        exchange.EnsureSuccessStatusCode();
        var result = await ExecuteOnceProjected(
            "/api/invitations/user-setup",
            new { invitationId = _invitationId, firstName = "Jane", lastName = "Doe", acceptedLegalTerms = false, acceptedLegalVersion = string.Empty },
            _owner);
        IsSuccess(result).ShouldBeTrue();
        await Host.WaitForFromAnte<InvitationToJoinTenantAccepted>(_invitationId.ToString());

        await using var stream = await Other.WatchInvitationStatus(_invitationId, _owner);
        _firstStreamedStatus = StatusOf(await stream.Next());
        _snapshotStatus = StatusOf(await Other.InvitationStatus(_invitationId, _owner));
        _strangerStatus = StatusOf(await Other.InvitationStatus(_invitationId, $"stranger-{Guid.NewGuid():N}"));
    }

    [Fact] void should_start_the_other_instances_subscription_at_the_terminal_status() => _firstStreamedStatus.ShouldEqual(Accepted);
    [Fact] void should_show_the_terminal_status_in_the_other_instances_snapshot() => _snapshotStatus.ShouldEqual(Accepted);
    [Fact] void should_not_show_a_stranger_the_terminal_status_on_the_other_instance() => _strangerStatus.ShouldEqual(0);

    static int StatusOf(JsonDocument frame) => frame.RootElement.GetProperty("data").GetProperty("status").GetInt32();
}
