// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Ante.Contracts.Legal;
using Ante.Integration.given;
using Ante.Legal;

namespace Ante.Integration.Invitations.when_an_invited_user_joins_an_organization;

[Collection(ChronicleCollection.Name)]
public class and_accepts_the_current_legal_terms : a_running_ante
{
    readonly Guid _invitationId = NewInvitationId();
    readonly string _subject = $"user-{Guid.NewGuid():N}";
    readonly UserInvitedToJoinTenant _invitation = JoinInvitation("Acme");
    HttpStatusCode _exchange;
    JsonDocument _result;
    InvitationToJoinTenantAccepted _accepted;
    LegalTermsAccepted _legal;

    protected override ILegalDocumentSource? LegalDocuments => new CurrentLegalDocuments();

    async Task Establish()
    {
        var issued = await Invite(Host, _invitationId, _invitation);
        using var response = await Ante.ExchangeInvitation(issued.Token, _subject);
        _exchange = response.StatusCode;
    }

    async Task Because()
    {
        _result = await ExecuteOnceProjected(
            "/api/invitations/user-setup",
            new { invitationId = _invitationId, firstName = "Jane", lastName = "Doe", acceptedLegalTerms = true, acceptedLegalVersion = CurrentLegalDocuments.Version.Value },
            _subject);
        _accepted = await Host.WaitForFromAnte<InvitationToJoinTenantAccepted>(_invitationId.ToString());
        _legal = await Host.WaitForFromAnte<LegalTermsAccepted>(_invitationId.ToString());
    }

    [Fact] void should_exchange_the_token_for_a_session() => _exchange.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_accept_the_command() => IsSuccess(_result).ShouldBeTrue();
    [Fact] void should_publish_the_acceptance_for_the_invited_tenant() => _accepted.TenantName.Value.ShouldEqual("Acme");
    [Fact] void should_publish_the_signed_in_subject() => _accepted.IdentityProviderSubject.ShouldEqual(_subject);
    [Fact] void should_publish_the_identity_provider() => _accepted.IdentityProvider.Value.ShouldEqual(AnteApplication.IdentityProvider);

    // [PII] values cross two stores (Ante's log/outbox, then the host inbox) and must read back decrypted.
    [Fact] void should_deliver_the_first_name_decrypted() => _accepted.FirstName.Value.ShouldEqual("Jane");
    [Fact] void should_deliver_the_invited_email_decrypted() => _accepted.Email.Value.ShouldEqual(_invitation.Email.Value);
    [Fact] void should_publish_the_legal_version_accepted() => _legal.Version.ShouldEqual(CurrentLegalDocuments.Version);
    [Fact] void should_record_legal_acceptance_for_the_same_subject() => _legal.IdentityProviderSubject.ShouldEqual(_subject);
}
