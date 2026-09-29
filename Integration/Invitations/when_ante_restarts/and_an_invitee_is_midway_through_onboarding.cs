// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Ante.Integration.given;
using Ante.Legal;

namespace Ante.Integration.Invitations.when_ante_restarts;

/// <summary>
/// An invitee who exchanged their link before a restart finishes onboarding after it (Cratis/Ante#67).
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_an_invitee_is_midway_through_onboarding : a_running_ante
{
    readonly Guid _invitationId = NewInvitationId();
    readonly string _subject = $"user-{Guid.NewGuid():N}";
    HttpStatusCode _exchange;
    JsonDocument _result;
    InvitationToJoinTenantAccepted _accepted;

    protected override ILegalDocumentSource? LegalDocuments => new CurrentLegalDocuments();

    async Task Establish()
    {
        var issued = await Invite(Host, _invitationId, JoinInvitation("Acme"));
        using var response = await Ante.ExchangeInvitation(issued.Token, _subject);
        _exchange = response.StatusCode;
    }

    async Task Because()
    {
        await Restart();
        _result = await ExecuteOnceProjected(
            "/api/invitations/user-setup",
            new { invitationId = _invitationId, firstName = "Jane", lastName = "Doe", acceptedLegalTerms = true, acceptedLegalVersion = CurrentLegalDocuments.Version.Value },
            _subject);
        _accepted = await Host.WaitForFromAnte<InvitationToJoinTenantAccepted>(_invitationId.ToString());
    }

    [Fact] void should_have_exchanged_the_link_before_the_restart() => _exchange.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_accept_the_onboarding_after_the_restart() => IsSuccess(_result).ShouldBeTrue();
    [Fact] void should_publish_the_acceptance() => _accepted.IdentityProviderSubject.ShouldEqual(_subject);
}
