// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Ante.Integration.given;
using Ante.Invitations.Receiving;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Integration.Invitations.when_two_ante_instances_share_a_store;

/// <summary>
/// The invitee submits the same join acceptance to both instances at once - a double submit that a load balancer
/// spreads over two replicas. The acceptance fence and the one-use constraint are enforced by the kernel, so exactly
/// one instance records it, the other is refused with a command result rather than an error, and the host receives
/// one acceptance (Cratis/Ante#119).
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_both_accept_the_same_invitation : two_running_antes
{
    readonly Guid _invitationId = NewInvitationId();
    readonly string _owner = $"owner-{Guid.NewGuid():N}";
    JsonDocument[] _results;
    IReadOnlyList<AppendedEvent> _recorded;
    IReadOnlyList<AppendedEvent> _published;
    int _receivedByHost;

    string Id => _invitationId.ToString("D");

    async Task Establish()
    {
        var issued = await Invite(Host, _invitationId, JoinInvitation());
        using var exchange = await Ante.ExchangeInvitation(issued.Token, _owner);
        exchange.EnsureSuccessStatusCode();

        // Both instances read the one pending invitation from the shared database; wait for it so neither attempt is
        // refused for projection lag instead of for losing the race.
        await Eventually.Until(
            async () =>
            {
                await using var scope = Other.Services.CreateAsyncScope();
                return await scope.ServiceProvider.GetRequiredService<IEventStore>().ReadModels.GetInstanceById<PendingInvitationToJoin>(_invitationId) is not null;
            },
            what: "the pending join invitation to be projected");
    }

    async Task Because()
    {
        var command = new { invitationId = _invitationId, firstName = "Jane", lastName = "Doe", acceptedLegalTerms = false, acceptedLegalVersion = string.Empty };
        _results = await Task.WhenAll(
            Ante.Execute("/api/invitations/user-setup", command, _owner),
            Other.Execute("/api/invitations/user-setup", command, _owner));

        await Host.WaitForFromAnte<InvitationToJoinTenantAccepted>(Id);
        _recorded = await EventsFor(Ante, Id, EventSequenceId.Log, typeof(InvitationToJoinTenantAccepted));
        _published = await EventsFor(Ante, Id, EventSequenceId.Outbox, typeof(InvitationToJoinTenantAccepted));
        _receivedByHost = (await Host.ReceivedFromAnte(Id)).Count(entry => entry.Content is InvitationToJoinTenantAccepted);
    }

    [Fact] void should_let_exactly_one_instance_accept() => _results.Count(IsSuccess).ShouldEqual(1);
    [Fact] void should_refuse_the_other_attempt_without_an_exception() => _results.Where(result => !IsSuccess(result)).All(HasNoExceptions).ShouldBeTrue();
    [Fact] void should_record_one_acceptance() => _recorded.Count.ShouldEqual(1);
    [Fact] void should_publish_one_acceptance() => _published.Count.ShouldEqual(1);
    [Fact] void should_deliver_one_acceptance_to_the_host() => _receivedByHost.ShouldEqual(1);

    static bool HasNoExceptions(JsonDocument result) =>
        !result.RootElement.TryGetProperty("hasExceptions", out var hasExceptions) || !hasExceptions.GetBoolean();
}
