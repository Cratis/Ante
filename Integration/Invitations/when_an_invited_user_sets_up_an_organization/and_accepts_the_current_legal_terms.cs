// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Ante.Contracts.Legal;
using Ante.Integration.given;
using Ante.Legal;

namespace Ante.Integration.Invitations.when_an_invited_user_sets_up_an_organization;

[Collection(ChronicleCollection.Name)]
public class and_accepts_the_current_legal_terms : a_running_ante
{
    readonly Guid _invitationId = NewInvitationId();
    readonly string _subject = $"owner-{Guid.NewGuid():N}";
    readonly string _organization = $"Org-{Guid.NewGuid():N}"[..20];
    readonly UserInvitedToCreateTenant _invitation = CreateInvitation();
    InvitationTokenIssued _issued;
    JsonDocument _result;
    InvitationToCreateTenantAccepted _accepted;
    LegalTermsAccepted _legal;

    protected override ILegalDocumentSource? LegalDocuments => new CurrentLegalDocuments();

    async Task Establish()
    {
        _issued = await Invite(Host, _invitationId, _invitation);
        using var _ = await Ante.ExchangeInvitation(_issued.Token, _subject);
    }

    async Task Because()
    {
        _result = await ExecuteOnceProjected(
            "/api/invitations/organization-setup",
            new { invitationId = _invitationId, organizationName = _organization, firstName = "Ada", middleName = "M", lastName = "Lovelace", acceptedLegalTerms = true, acceptedLegalVersion = CurrentLegalDocuments.Version.Value },
            _subject);
        _accepted = await Host.WaitForFromAnte<InvitationToCreateTenantAccepted>(_invitationId.ToString());
        _legal = await Host.WaitForFromAnte<LegalTermsAccepted>(_invitationId.ToString());
    }

    [Fact] void should_issue_a_create_tenant_token() => _issued.FlowType.ShouldEqual(InvitationFlowType.CreateTenant);
    [Fact] void should_accept_the_command() => IsSuccess(_result).ShouldBeTrue();
    [Fact] void should_publish_the_organization_name() => _accepted.TenantName.Value.ShouldEqual(_organization);
    [Fact] void should_publish_the_signed_in_subject() => _accepted.IdentityProviderSubject.ShouldEqual(_subject);
    [Fact] void should_deliver_the_names_decrypted() => $"{_accepted.FirstName.Value} {_accepted.MiddleName.Value} {_accepted.LastName.Value}".ShouldEqual("Ada M Lovelace");
    [Fact] void should_deliver_the_invited_email_decrypted() => _accepted.Email.Value.ShouldEqual(_invitation.Email.Value);
    [Fact] void should_publish_legal_acceptance_for_the_new_organization() => _legal.TenantName.Value.ShouldEqual(_organization);
}
