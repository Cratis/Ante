// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Ante.Integration.given;
using Ante.Invitations.UserSetup;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Integration.Invitations.when_ante_restarts;

/// <summary>
/// Ante stops after recording a join acceptance but before its outbox reactor published it; the restarted instance
/// publishes it once, and only the owner sees the terminal status (Cratis/Ante#112).
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_an_acceptance_was_recorded_but_not_published : a_running_ante
{
    readonly Guid _invitationId = NewInvitationId();
    readonly string _owner = $"owner-{Guid.NewGuid():N}";
    bool _recordedBeforePublication;
    int _acceptances;
    int _ownerStatus;
    int _strangerStatus;

    async Task Establish()
    {
        var issued = await Invite(Host, _invitationId, JoinInvitation());
        using var response = await Ante.ExchangeInvitation(issued.Token, _owner);
        response.EnsureSuccessStatusCode();

        // Disconnect the outbox reactor before accepting. The kernel keeps its cursor, and the restarted Ante
        // registers a fresh handler for the same reactor.
        await using var scope = Ante.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<IEventStore>().Reactors.GetHandlerFor<JoinTenantAcceptanceOutbox>();
        handler.Disconnect();
        await Eventually.Until(async () => !(await handler.GetState()).IsSubscribed, what: "the join acceptance outbox reactor disconnecting");

        var result = await ExecuteOnceProjected("/api/invitations/user-setup", new { invitationId = _invitationId, firstName = "Jane", lastName = "Doe", acceptedLegalTerms = false, acceptedLegalVersion = "" }, _owner);
        IsSuccess(result).ShouldBeTrue();
        _recordedBeforePublication = await AcceptanceRecordedButNotPublished<InvitationToJoinTenantAccepted>(_invitationId);
    }

    async Task Because()
    {
        await Restart();
        await Host.WaitForFromAnte<InvitationToJoinTenantAccepted>(_invitationId.ToString());
        _acceptances = (await Host.ReceivedFromAnte(_invitationId.ToString())).Count(entry => entry.Content is InvitationToJoinTenantAccepted);
        await Eventually.Until(
            async () => (_ownerStatus = Status(await Ante.InvitationStatus(_invitationId, _owner))) == 2,
            what: "the owner's terminal status after the restart");
        _strangerStatus = Status(await Ante.InvitationStatus(_invitationId, $"stranger-{Guid.NewGuid():N}"));
    }

    static int Status(JsonDocument result) => result.RootElement.GetProperty("data").GetProperty("status").GetInt32();

    [Fact] void should_have_recorded_the_acceptance_without_publishing_it_before_the_restart() => _recordedBeforePublication.ShouldBeTrue();
    [Fact] void should_publish_the_acceptance_once() => _acceptances.ShouldEqual(1);
    [Fact] void should_show_the_owner_the_terminal_status() => _ownerStatus.ShouldEqual(2);
    [Fact] void should_not_show_a_stranger_the_terminal_status() => _strangerStatus.ShouldEqual(0);
}
