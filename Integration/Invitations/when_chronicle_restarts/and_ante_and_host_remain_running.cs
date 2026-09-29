// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Ante.Integration.given;

namespace Ante.Integration.Invitations.when_chronicle_restarts;

/// <summary>
/// The kernel restarts under a running Ante, its host and an invitee watching their acceptance status: Ante reports
/// itself unready during the outage, then resumes its inbox and outbox reactors without reissuing earlier tokens,
/// and the invitee's existing subscription sees the acceptance they submit after the restart (Cratis/Ante#112).
/// </summary>
/// <remarks>
/// One class covers the whole restart because each kernel restart reloads every store the suite has created so
/// far, which makes it the most expensive step in the suite.
/// </remarks>
[Collection(ChronicleCollection.Name)]
public class and_ante_and_host_remain_running : a_running_ante
{
    readonly Guid _watchedInvitationId = NewInvitationId();
    readonly Guid _laterInvitationId = NewInvitationId();
    readonly string _owner = $"owner-{Guid.NewGuid():N}";
    StatusStream _stream;
    int _firstStatus;
    HttpStatusCode _readinessDuringOutage;
    InvitationTokenIssued _issuedAfter;
    int _earlierTokens;
    JsonDocument _result;
    InvitationToJoinTenantAccepted _accepted;
    int _terminalStatus = -1;

    async Task Establish()
    {
        var issued = await Invite(Host, _watchedInvitationId, JoinInvitation());
        using var exchange = await Ante.ExchangeInvitation(issued.Token, _owner);
        exchange.EnsureSuccessStatusCode();
        _stream = await Ante.WatchInvitationStatus(_watchedInvitationId, _owner);
        _firstStatus = Status(await _stream.Next());
    }

    async Task Because()
    {
        _readinessDuringOutage = await RestartChronicle();

        await Host.Publish(_laterInvitationId, JoinInvitation(), subject: Guid.NewGuid());
        _issuedAfter = await Host.WaitForFromAnte<InvitationTokenIssued>(_laterInvitationId.ToString());
        _earlierTokens = (await Host.ReceivedFromAnte(_watchedInvitationId.ToString())).Count(entry => entry.Content is InvitationTokenIssued);

        _result = await ExecuteOnceProjected(
            "/api/invitations/user-setup",
            new { invitationId = _watchedInvitationId, firstName = "Jane", lastName = "Doe", acceptedLegalTerms = false, acceptedLegalVersion = "" },
            _owner);
        _accepted = await Host.WaitForFromAnte<InvitationToJoinTenantAccepted>(_watchedInvitationId.ToString());
        var deadline = DateTimeOffset.UtcNow + Eventually.DefaultTimeout;
        while (_terminalStatus != 2)
        {
            var remaining = deadline - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            try
            {
                _terminalStatus = Status(await _stream.Next(remaining));
            }
            catch (OperationCanceledException)
            {
                // The subscription delivered nothing more in time: the assertion reports it.
                break;
            }
        }
    }

    async Task Destroy()
    {
        if (_stream is not null)
        {
            await _stream.DisposeAsync();
        }
    }

    static int Status(JsonDocument frame) => frame.RootElement.GetProperty("data").GetProperty("status").GetInt32();

    [Fact] void should_lose_readiness_during_the_outage() => _readinessDuringOutage.ShouldEqual(HttpStatusCode.ServiceUnavailable);
    [Fact] void should_resume_the_inbox_reactor() => _issuedAfter.Token.ShouldNotBeEmpty();
    [Fact] void should_not_reissue_the_earlier_token() => _earlierTokens.ShouldEqual(1);
    [Fact] void should_show_the_watching_owner_a_pending_status_before_the_restart() => _firstStatus.ShouldEqual(0);
    [Fact] void should_accept_the_onboarding_after_the_restart() => IsSuccess(_result).ShouldBeTrue();
    [Fact] void should_publish_the_acceptance() => _accepted.IdentityProviderSubject.ShouldEqual(_owner);
    [Fact] void should_deliver_the_terminal_status_to_the_existing_subscription() => _terminalStatus.ShouldEqual(2);
}
