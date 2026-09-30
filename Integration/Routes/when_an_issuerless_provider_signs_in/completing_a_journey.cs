// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Ante.Contracts.Invitations;
using Ante.Contracts.Organization;
using Ante.Integration.given;
using Ante.Integration.Routes.given;

namespace Ante.Integration.Routes.when_an_issuerless_provider_signs_in;

/// <summary>
/// A deployment whose one provider has no issuer (an OAuth2-only provider such as GitHub) receives sign-ins that report
/// nothing about their provider. Ante attributes them to the configured provider, so registering and accepting an
/// invitation complete and are published under its name; a sign-in that only carries the OIDC federation marker cannot
/// be attributed and is refused. Shared by every cell of the route matrix.
/// </summary>
public abstract class completing_a_journey : a_routed_ante
{
    const string Federation = "AuthenticationTypes.Federation";

    readonly Guid _registration = Guid.NewGuid();
    readonly Guid _invitation = Guid.NewGuid();
    readonly Guid _markedRegistration = Guid.NewGuid();
    Reply _start;
    Reply _register;
    Reply _status;
    Reply _exchange;
    Reply _accept;
    Reply _markedStart;
    Reply _markedExchange;
    OrganizationRegistrationCompleted _registered;
    InvitationToJoinTenantAccepted _accepted;

    protected override bool ReceivesInvitations => true;

    async Task Because()
    {
        var visitor = $"visitor-{Suffix}";
        var invitee = $"invitee-{Suffix}";

        _start = await Send(HttpMethod.Post, "/api/organization/registration/start", visitor, provider: null, body: new { registrationId = _registration });
        _register = await Send(HttpMethod.Post, "/api/organization/registration", visitor, provider: null, body: new { registrationId = _registration, organizationName = $"Self{Suffix}", firstName = "Grace", lastName = "Hopper", acceptedLegalTerms = false, acceptedLegalVersion = string.Empty });
        _registered = await Host.WaitForFromAnte<OrganizationRegistrationCompleted>(_registration.ToString());
        await Eventually.Until(
            async () => StatusOf((_status = await Send(HttpMethod.Get, $"/api/invitations/organization-setup/status-for-registration?registrationId={_registration:D}", visitor, provider: null)).Json) == 2,
            what: "the issuerless visitor's registration status");

        var token = await Invite(Host, _invitation, JoinInvitation());
        _exchange = await Exchange(token.Token, invitee, provider: null);
        _accept = await SendUntilSuccess("/api/invitations/user-setup", invitee, new { invitationId = _invitation, firstName = "Jane", lastName = "Doe", acceptedLegalTerms = false, acceptedLegalVersion = string.Empty }, provider: null);
        _accepted = await Host.WaitForFromAnte<InvitationToJoinTenantAccepted>(_invitation.ToString());

        _markedStart = await Send(HttpMethod.Post, "/api/organization/registration/start", $"marked-{Suffix}", Federation, body: new { registrationId = _markedRegistration });
        _markedExchange = await Exchange(token.Token, $"marked-{Suffix}", Federation);
    }

    [Fact] public void should_let_the_visitor_start_a_registration() => (_start.IsOk && _start.IsSuccess).ShouldBeTrue();
    [Fact] public void should_let_the_visitor_complete_the_registration() => (_register.IsOk && _register.IsSuccess).ShouldBeTrue();
    [Fact] public void should_publish_the_registration_under_the_configured_provider() => _registered.IdentityProvider.Value.ShouldEqual(AnteApplication.IdentityProvider);
    [Fact] public void should_report_the_registration_as_accepted_to_the_visitor() => StatusOf(_status.Json).ShouldEqual(2);
    [Fact] public void should_exchange_the_invitation_for_the_invitee() => _exchange.Status.ShouldEqual(HttpStatusCode.OK);
    [Fact] public void should_let_the_invitee_accept_the_invitation() => (_accept.IsOk && _accept.IsSuccess).ShouldBeTrue();
    [Fact] public void should_publish_the_acceptance_under_the_configured_provider() => _accepted.IdentityProvider.Value.ShouldEqual(AnteApplication.IdentityProvider);
    [Fact] public void should_refuse_a_registration_that_only_carries_the_federation_marker() => (_markedStart.Status == HttpStatusCode.BadRequest && !_markedStart.IsSuccess).ShouldBeTrue();
    [Fact] public void should_refuse_an_exchange_that_only_carries_the_federation_marker() => _markedExchange.Status.ShouldEqual(HttpStatusCode.BadRequest);
}
