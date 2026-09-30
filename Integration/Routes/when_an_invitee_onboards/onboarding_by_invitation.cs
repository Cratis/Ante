// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Ante.Contracts.Invitations;
using Ante.Integration.given;
using Ante.Integration.Routes.given;

namespace Ante.Integration.Routes.when_an_invitee_onboards;

/// <summary>
/// Nothing under <c>/api</c> is marked authorized; the onboarding commands and status queries are gated by whether the
/// forwarded sign-in owns the invitation. An anonymous caller and a signed-in stranger are refused exactly like an
/// unknown invitation and read a pending status; only the invitee whose token was exchanged proceeds and sees progress,
/// including on a status stream that reconnects. Shared by every cell of the route matrix.
/// </summary>
public abstract class onboarding_by_invitation : a_routed_ante
{
    const int Pending = 0;
    const int Accepted = 2;
    const string PendingJoin = "/api/invitations/receiving/pending-join-for-current-invitee";
    const string PendingSetup = "/api/invitations/receiving/pending-create-organization-for-current-invitee";

    readonly Guid _join = Guid.NewGuid();
    readonly Guid _create = Guid.NewGuid();
    string _joiner;
    string _creator;
    string _stranger;
    string _organization;

    Reply _pendingJoinToJoiner;
    Reply _pendingJoinToStranger;
    Reply _pendingJoinToAnonymous;
    Reply _pendingSetupToCreator;
    Reply _pendingSetupToStranger;
    Reply _joinByAnonymous;
    Reply _joinByStranger;
    Reply _joinByCreator;
    Reply _setupByAnonymous;
    Reply _setupByStranger;
    Reply _setupByJoiner;
    Reply _joinByJoiner;
    Reply _setupByCreator;
    int _joinStatusBeforeToJoiner;
    int _joinStatusBeforeToStranger;
    int _joinStatusToJoiner;
    int _joinStatusToStranger;
    int _joinStatusToAnonymous;
    int _setupStatusToCreator;
    int _setupStatusToStranger;
    int _setupStatusToAnonymous;
    int _firstStreamFrame;
    int _lastStreamFrame;
    int _reconnectedFrame;
    int _reconnectedStrangerFrame;
    int _reconnectedAnonymousFrame;

    protected override bool ReceivesInvitations => true;

    string JoinStatus => $"/api/invitations/user-setup/status-for-invitation?invitationId={_join:D}";

    string SetupStatus => $"/api/invitations/organization-setup/status-for-invitation?invitationId={_create:D}";

    async Task Because()
    {
        _joiner = $"joiner-{Suffix}";
        _creator = $"creator-{Suffix}";
        _stranger = $"stranger-{Suffix}";
        _organization = $"Org{Suffix}";

        var joinToken = await Invite(Host, _join, JoinInvitation());
        var createToken = await Invite(Host, _create, CreateInvitation());
        (await Exchange(joinToken.Token, _joiner)).Status.ShouldEqual(HttpStatusCode.OK);
        (await Exchange(createToken.Token, _creator)).Status.ShouldEqual(HttpStatusCode.OK);

        // The token reaches the host before the pending projection is readable; the owner's own query says when it is,
        // so the refusals below are the ownership check and never a projection that lags.
        _pendingJoinToJoiner = await Eventually.Get(async () => await ReadPending(PendingJoin, _joiner), what: "the pending join invitation");
        _pendingSetupToCreator = await Eventually.Get(async () => await ReadPending(PendingSetup, _creator), what: "the pending create-organization invitation");
        _pendingJoinToStranger = await Send(HttpMethod.Get, PendingJoin, _stranger);
        _pendingJoinToAnonymous = await Send(HttpMethod.Get, PendingJoin);
        _pendingSetupToStranger = await Send(HttpMethod.Get, PendingSetup, _stranger);

        _joinByAnonymous = await Send(HttpMethod.Post, "/api/invitations/user-setup", body: JoinCommand());
        _joinByStranger = await Send(HttpMethod.Post, "/api/invitations/user-setup", _stranger, body: JoinCommand());
        _joinByCreator = await Send(HttpMethod.Post, "/api/invitations/user-setup", _creator, body: JoinCommand());
        _setupByAnonymous = await Send(HttpMethod.Post, "/api/invitations/organization-setup", body: SetupCommand());
        _setupByStranger = await Send(HttpMethod.Post, "/api/invitations/organization-setup", _stranger, body: SetupCommand());
        _setupByJoiner = await Send(HttpMethod.Post, "/api/invitations/organization-setup", _joiner, body: SetupCommand());

        _joinStatusBeforeToJoiner = StatusOf((await Send(HttpMethod.Get, JoinStatus, _joiner)).Json);
        _joinStatusBeforeToStranger = StatusOf((await Send(HttpMethod.Get, JoinStatus, _stranger)).Json);

        await using (var stream = await Watch(JoinStatus, _joiner))
        {
            _firstStreamFrame = StatusOf((await stream.Next()).RootElement);

            _joinByJoiner = await SendUntilSuccess("/api/invitations/user-setup", _joiner, JoinCommand());
            _setupByCreator = await SendUntilSuccess("/api/invitations/organization-setup", _creator, SetupCommand());
            await Host.WaitForFromAnte<InvitationToJoinTenantAccepted>(_join.ToString());
            await Host.WaitForFromAnte<InvitationToCreateTenantAccepted>(_create.ToString());

            _lastStreamFrame = _firstStreamFrame;
            while (_lastStreamFrame != Accepted)
            {
                _lastStreamFrame = StatusOf((await stream.Next()).RootElement);
            }
        }

        // A dropped connection reconnects to whatever is current, not to the pending state it first saw.
        await using (var stream = await Watch(JoinStatus, _joiner))
        {
            _reconnectedFrame = StatusOf((await stream.Next()).RootElement);
        }

        await using (var stream = await Watch(JoinStatus, _stranger))
        {
            _reconnectedStrangerFrame = StatusOf((await stream.Next()).RootElement);
        }

        await using (var stream = await Watch(JoinStatus, null))
        {
            _reconnectedAnonymousFrame = StatusOf((await stream.Next()).RootElement);
        }

        _joinStatusToJoiner = StatusOf((await Send(HttpMethod.Get, JoinStatus, _joiner)).Json);
        _joinStatusToStranger = StatusOf((await Send(HttpMethod.Get, JoinStatus, _stranger)).Json);
        _joinStatusToAnonymous = StatusOf((await Send(HttpMethod.Get, JoinStatus)).Json);
        await Eventually.Until(async () => (_setupStatusToCreator = StatusOf((await Send(HttpMethod.Get, SetupStatus, _creator)).Json)) == Accepted, what: "the owner's create-organization status");
        _setupStatusToStranger = StatusOf((await Send(HttpMethod.Get, SetupStatus, _stranger)).Json);
        _setupStatusToAnonymous = StatusOf((await Send(HttpMethod.Get, SetupStatus)).Json);
    }

    // Owner-only reads: the invitee sees the pending invitation, everyone else gets nothing.
    [Fact] public void should_show_the_invitee_the_pending_join_invitation() => _pendingJoinToJoiner.Data.GetProperty("tenantName").GetString().ShouldEqual("Acme");
    [Fact] public void should_show_the_invitee_the_pending_create_organization_invitation() => _pendingSetupToCreator.Data.GetProperty("id").GetGuid().ShouldEqual(_create);
    [Fact] public void should_show_a_stranger_no_pending_join_invitation() => IsNothing(_pendingJoinToStranger).ShouldBeTrue();
    [Fact] public void should_show_an_anonymous_visitor_no_pending_join_invitation() => IsNothing(_pendingJoinToAnonymous).ShouldBeTrue();
    [Fact] public void should_show_a_stranger_no_pending_create_organization_invitation() => IsNothing(_pendingSetupToStranger).ShouldBeTrue();

    // Commands: refused as an unknown invitation unless the caller owns it, whoever else is signed in.
    [Fact] public void should_refuse_an_anonymous_visitor_accepting_the_join_invitation() => IsRefused(_joinByAnonymous).ShouldBeTrue();
    [Fact] public void should_refuse_a_stranger_accepting_the_join_invitation() => IsRefused(_joinByStranger).ShouldBeTrue();
    [Fact] public void should_refuse_the_owner_of_another_invitation_accepting_the_join_invitation() => IsRefused(_joinByCreator).ShouldBeTrue();
    [Fact] public void should_refuse_an_anonymous_visitor_setting_up_the_organization() => IsRefused(_setupByAnonymous).ShouldBeTrue();
    [Fact] public void should_refuse_a_stranger_setting_up_the_organization() => IsRefused(_setupByStranger).ShouldBeTrue();
    [Fact] public void should_refuse_the_owner_of_another_invitation_setting_up_the_organization() => IsRefused(_setupByJoiner).ShouldBeTrue();
    [Fact] public void should_let_the_invitee_accept_the_join_invitation() => _joinByJoiner.IsSuccess.ShouldBeTrue();
    [Fact] public void should_let_the_invitee_set_up_the_organization() => _setupByCreator.IsSuccess.ShouldBeTrue();

    // Status: pending for everyone until the owner's acceptance reaches the outbox, then accepted for the owner alone.
    [Fact] public void should_report_pending_to_the_invitee_before_accepting() => _joinStatusBeforeToJoiner.ShouldEqual(Pending);
    [Fact] public void should_report_pending_to_a_stranger_before_the_invitee_accepts() => _joinStatusBeforeToStranger.ShouldEqual(Pending);
    [Fact] public void should_report_accepted_to_the_invitee_afterwards() => _joinStatusToJoiner.ShouldEqual(Accepted);
    [Fact] public void should_still_report_pending_to_a_stranger_afterwards() => _joinStatusToStranger.ShouldEqual(Pending);
    [Fact] public void should_still_report_pending_to_an_anonymous_visitor_afterwards() => _joinStatusToAnonymous.ShouldEqual(Pending);
    [Fact] public void should_report_accepted_organization_setup_to_the_invitee() => _setupStatusToCreator.ShouldEqual(Accepted);
    [Fact] public void should_report_pending_organization_setup_to_a_stranger() => _setupStatusToStranger.ShouldEqual(Pending);
    [Fact] public void should_report_pending_organization_setup_to_an_anonymous_visitor() => _setupStatusToAnonymous.ShouldEqual(Pending);

    // The live status stream.
    [Fact] public void should_open_the_status_stream_on_the_current_status() => _firstStreamFrame.ShouldEqual(Pending);
    [Fact] public void should_deliver_the_accepted_status_to_the_open_stream() => _lastStreamFrame.ShouldEqual(Accepted);
    [Fact] public void should_give_a_reconnecting_invitee_the_current_status() => _reconnectedFrame.ShouldEqual(Accepted);
    [Fact] public void should_give_a_reconnecting_stranger_nothing_but_pending() => _reconnectedStrangerFrame.ShouldEqual(Pending);
    [Fact] public void should_give_a_reconnecting_anonymous_visitor_nothing_but_pending() => _reconnectedAnonymousFrame.ShouldEqual(Pending);

    static bool IsNothing(Reply reply) =>
        reply.IsOk && reply.IsSuccess && (!reply.Json.TryGetProperty("data", out var data) || data.ValueKind == JsonValueKind.Null);

    static bool IsRefused(Reply reply) => reply.Status == HttpStatusCode.BadRequest && !reply.IsSuccess;

    object JoinCommand() => new { invitationId = _join, firstName = "Jane", lastName = "Doe", acceptedLegalTerms = false, acceptedLegalVersion = string.Empty };

    object SetupCommand() => new { invitationId = _create, organizationName = _organization, firstName = "Jane", lastName = "Doe", acceptedLegalTerms = false, acceptedLegalVersion = string.Empty };

    async Task<Reply?> ReadPending(string path, string subject)
    {
        var reply = await Send(HttpMethod.Get, path, subject);
        return reply.IsOk && reply.IsSuccess && reply.Json.GetProperty("data").ValueKind == JsonValueKind.Object ? reply : null;
    }
}
