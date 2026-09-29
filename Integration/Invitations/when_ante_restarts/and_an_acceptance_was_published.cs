// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;

namespace Ante.Integration.Invitations.when_ante_restarts;

/// <summary>
/// An acceptance published before a restart stays terminal for its owner afterwards and is neither republished nor
/// followed by a new token (Cratis/Ante#112).
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_an_acceptance_was_published : a_running_ante
{
    readonly Guid _invitationId = NewInvitationId();
    readonly string _owner = $"owner-{Guid.NewGuid():N}";
    int _status;
    int _tokens;
    int _acceptances;

    async Task Establish()
    {
        var issued = await Invite(Host, _invitationId, JoinInvitation());
        using var response = await Ante.ExchangeInvitation(issued.Token, _owner);
        response.EnsureSuccessStatusCode();
        var result = await ExecuteOnceProjected("/api/invitations/user-setup", new { invitationId = _invitationId, firstName = "Jane", lastName = "Doe", acceptedLegalTerms = false, acceptedLegalVersion = "" }, _owner);
        IsSuccess(result).ShouldBeTrue();
        await Host.WaitForFromAnte<InvitationToJoinTenantAccepted>(_invitationId.ToString());
    }

    async Task Because()
    {
        await Restart();
        _status = (await Ante.InvitationStatus(_invitationId, _owner)).RootElement.GetProperty("data").GetProperty("status").GetInt32();
        var received = await Host.ReceivedFromAnte(_invitationId.ToString());
        _tokens = received.Count(entry => entry.Content is InvitationTokenIssued);
        _acceptances = received.Count(entry => entry.Content is InvitationToJoinTenantAccepted);
    }

    [Fact] void should_keep_the_terminal_status_for_the_owner() => _status.ShouldEqual(2);
    [Fact] void should_not_issue_a_new_token() => _tokens.ShouldEqual(1);
    [Fact] void should_not_publish_a_second_acceptance() => _acceptances.ShouldEqual(1);
}
