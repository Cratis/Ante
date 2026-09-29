// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Legal;
using Ante.Integration.given;
using Ante.Invitations;
using Ante.Invitations.Receiving;
using Ante.Legal;

namespace Ante.Integration.Invitations.when_setting_up_an_invited_organization_end_to_end;

/// <summary>
/// One invitation to set up an organization, followed from the host's outbox to the host's receipt of the acceptance, with
/// every hop read back through Chronicle and compared with the one before it.
/// </summary>
[Collection(ChronicleCollection.Name)]
public class from_host_outbox_to_host_receipt : a_running_ante
{
    readonly Guid _invitationId = NewInvitationId();
    readonly Guid _hostSubject = Guid.NewGuid();
    readonly Guid _invitationCorrelation = Guid.NewGuid();
    readonly Guid _acceptanceCorrelation = Guid.NewGuid();
    readonly string _organization = $"Org-{Guid.NewGuid():N}"[..20];
    readonly string _subject = $"owner-{Guid.NewGuid():N}";
    readonly UserInvitedToCreateTenant _invitation = new($"{Guid.NewGuid():N}@example.com", ["owner", "billing"]);
    OnboardingTrace _trace;
    InvitationTokenIssued _issued;

    protected override ILegalDocumentSource? LegalDocuments => new CurrentLegalDocuments();

    string Id => _invitationId.ToString("D");

    InvitationToCreateTenantAccepted ExpectedAcceptance =>
        new(_organization, AnteApplication.IdentityProvider, _subject, "Ada", "Byron", "Lovelace", _invitation.Email, ["owner", "billing"]);

    LegalTermsAccepted ExpectedLegal => new(_organization, AnteApplication.IdentityProvider, _subject, CurrentLegalDocuments.Version);

    async Task Because()
    {
        await Host.Publish(_invitationId, _invitation, _hostSubject, _invitationCorrelation);
        _issued = await Host.WaitForFromAnte<InvitationTokenIssued>(Id);
        using var exchange = await Ante.ExchangeInvitation(_issued.Token, _subject);
        await ExecuteOnceProjected(
            "/api/invitations/organization-setup",
            new { invitationId = _invitationId, organizationName = _organization, firstName = "Ada", middleName = "Byron", lastName = "Lovelace", acceptedLegalTerms = true, acceptedLegalVersion = CurrentLegalDocuments.Version.Value },
            _subject,
            correlationId: _acceptanceCorrelation);
        _trace = await OnboardingTrace.CaptureSettled(Ante, Host, Id, trace => trace.HostReceipt.Count >= 3);
    }

    // Counts per hop: one of each fact, nothing duplicated and nothing extra.
    [Fact] void should_publish_one_invitation_from_the_host() => OnboardingTrace.Types(_trace.HostOutbox).ShouldEqual(nameof(UserInvitedToCreateTenant));
    [Fact] void should_receive_the_invitation_once_in_the_ante_inbox() => OnboardingTrace.Types(_trace.AnteInbox).ShouldEqual(nameof(UserInvitedToCreateTenant));
    [Fact] void should_record_the_receipt_and_the_acceptance_once_in_the_ante_log() =>
        OnboardingTrace.Types(_trace.AnteLog).ShouldEqual(string.Join(", ", new[]
        {
            nameof(CreateTenantInvitationReceived), nameof(InvitationSourceInboxEventRecorded), nameof(InvitationToCreateTenantAccepted),
            nameof(LegalTermsAccepted), nameof(OnboardingAttemptClaimed),
        }));
    [Fact] void should_publish_the_token_the_acceptance_and_the_legal_acceptance_once_from_ante() =>
        OnboardingTrace.Types(_trace.AnteOutbox).ShouldEqual(string.Join(", ", nameof(InvitationToCreateTenantAccepted), nameof(InvitationTokenIssued), nameof(LegalTermsAccepted)));
    [Fact] void should_deliver_exactly_what_ante_published_to_the_host() =>
        OnboardingTrace.Types(_trace.HostReceipt).ShouldEqual(OnboardingTrace.Types(_trace.AnteOutbox));

    // Event source ids: one onboarding, one stream, at every hop.
    [Fact] void should_keep_the_invitation_id_as_the_event_source_id_at_every_hop() =>
        new[] { _trace.HostOutbox, _trace.AnteInbox, _trace.AnteLog, _trace.AnteOutbox, _trace.HostReceipt }
            .SelectMany(hop => hop).All(appended => appended.Context.EventSourceId.Value == Id).ShouldBeTrue();

    // Payloads: every property of the invitation, hop by hop.
    [Fact] void should_publish_the_complete_invitation_from_the_host() =>
        OnboardingTrace.Flatten(OnboardingTrace.TheOne<UserInvitedToCreateTenant>(_trace.HostOutbox).Content).ShouldEqual(OnboardingTrace.Flatten(_invitation));
    [Fact] void should_receive_the_complete_invitation_in_the_ante_inbox() =>
        OnboardingTrace.Flatten(OnboardingTrace.TheOne<UserInvitedToCreateTenant>(_trace.AnteInbox).Content).ShouldEqual(OnboardingTrace.Flatten(_invitation));
    [Fact] void should_record_the_complete_invitation_in_the_ante_log() =>
        OnboardingTrace.Flatten(OnboardingTrace.TheOne<CreateTenantInvitationReceived>(_trace.AnteLog).Content)
            .ShouldEqual(OnboardingTrace.Flatten(new CreateTenantInvitationReceived(_invitation.Email, _invitation.Roles)));

    // Payloads: the token, from Ante's outbox to the host.
    [Fact] void should_issue_a_create_tenant_token() => OnboardingTrace.TheOne<InvitationTokenIssued>(_trace.AnteOutbox).Content.ShouldBeOfExactType<InvitationTokenIssued>();
    [Fact] void should_publish_the_token_flow_type() => _issued.FlowType.ShouldEqual(InvitationFlowType.CreateTenant);
    [Fact] void should_deliver_the_token_as_ante_published_it() =>
        OnboardingTrace.Flatten(OnboardingTrace.TheOne<InvitationTokenIssued>(_trace.HostReceipt).Content).ShouldEqual(OnboardingTrace.Flatten(OnboardingTrace.TheOne<InvitationTokenIssued>(_trace.AnteOutbox).Content));
    [Fact] void should_bind_the_token_to_the_invitation_id() => OnboardingTrace.TokenClaims(_issued.Token).GetProperty("jti").GetString().ShouldEqual(Id);
    [Fact] void should_bind_the_token_to_the_create_tenant_flow() => OnboardingTrace.TokenClaims(_issued.Token).GetProperty("invite_type").GetString()!.ToLowerInvariant().ShouldContain("create");
    [Fact] void should_publish_the_token_expiry_the_token_carries() =>
        _issued.ExpiresAt.ToUnixTimeSeconds().ShouldEqual(OnboardingTrace.TokenClaims(_issued.Token).GetProperty("exp").GetInt64());

    // Payloads: the acceptance and the legal acceptance are identical at the log, the outbox and the host.
    [Fact] void should_record_the_complete_acceptance_in_the_ante_log() =>
        OnboardingTrace.Flatten(OnboardingTrace.TheOne<InvitationToCreateTenantAccepted>(_trace.AnteLog).Content).ShouldEqual(OnboardingTrace.Flatten(ExpectedAcceptance));
    [Fact] void should_publish_the_complete_acceptance_from_ante() =>
        OnboardingTrace.Flatten(OnboardingTrace.TheOne<InvitationToCreateTenantAccepted>(_trace.AnteOutbox).Content).ShouldEqual(OnboardingTrace.Flatten(ExpectedAcceptance));
    [Fact] void should_deliver_the_complete_acceptance_to_the_host() =>
        OnboardingTrace.Flatten(OnboardingTrace.TheOne<InvitationToCreateTenantAccepted>(_trace.HostReceipt).Content).ShouldEqual(OnboardingTrace.Flatten(ExpectedAcceptance));
    [Fact] void should_record_the_complete_legal_acceptance_in_the_ante_log() =>
        OnboardingTrace.Flatten(OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.AnteLog).Content).ShouldEqual(OnboardingTrace.Flatten(ExpectedLegal));
    [Fact] void should_publish_the_complete_legal_acceptance_from_ante() =>
        OnboardingTrace.Flatten(OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.AnteOutbox).Content).ShouldEqual(OnboardingTrace.Flatten(ExpectedLegal));
    [Fact] void should_deliver_the_complete_legal_acceptance_to_the_host() =>
        OnboardingTrace.Flatten(OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.HostReceipt).Content).ShouldEqual(OnboardingTrace.Flatten(ExpectedLegal));

    // Correlation ids: the invitation's travels from the host to the token it is answered with.
    [Fact] void should_carry_the_host_correlation_id_into_the_ante_inbox() => OnboardingTrace.TheOne<UserInvitedToCreateTenant>(_trace.AnteInbox).Context.CorrelationId.Value.ShouldEqual(_invitationCorrelation);
    [Fact] void should_carry_the_host_correlation_id_into_the_receipt() => OnboardingTrace.TheOne<CreateTenantInvitationReceived>(_trace.AnteLog).Context.CorrelationId.Value.ShouldEqual(_invitationCorrelation);
    [Fact] void should_answer_with_a_token_under_the_host_correlation_id() => OnboardingTrace.TheOne<InvitationTokenIssued>(_trace.AnteOutbox).Context.CorrelationId.Value.ShouldEqual(_invitationCorrelation);
    [Fact] void should_deliver_the_token_under_the_host_correlation_id() => OnboardingTrace.TheOne<InvitationTokenIssued>(_trace.HostReceipt).Context.CorrelationId.Value.ShouldEqual(_invitationCorrelation);

    // Correlation ids: the acceptance keeps the one its command ran under from the log to the host.
    [Fact] void should_record_the_acceptance_under_its_command_correlation_id() => OnboardingTrace.TheOne<InvitationToCreateTenantAccepted>(_trace.AnteLog).Context.CorrelationId.Value.ShouldEqual(_acceptanceCorrelation);
    [Fact] void should_publish_the_acceptance_under_the_same_correlation_id() => OnboardingTrace.TheOne<InvitationToCreateTenantAccepted>(_trace.AnteOutbox).Context.CorrelationId.Value.ShouldEqual(_acceptanceCorrelation);
    [Fact] void should_deliver_the_acceptance_under_the_same_correlation_id() => OnboardingTrace.TheOne<InvitationToCreateTenantAccepted>(_trace.HostReceipt).Context.CorrelationId.Value.ShouldEqual(_acceptanceCorrelation);
    [Fact] void should_record_the_legal_acceptance_under_the_same_correlation_id() => OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.AnteLog).Context.CorrelationId.Value.ShouldEqual(_acceptanceCorrelation);
    [Fact] void should_publish_the_legal_acceptance_under_the_same_correlation_id() => OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.AnteOutbox).Context.CorrelationId.Value.ShouldEqual(_acceptanceCorrelation);
    [Fact] void should_deliver_the_legal_acceptance_under_the_same_correlation_id() => OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.HostReceipt).Context.CorrelationId.Value.ShouldEqual(_acceptanceCorrelation);

    // Compliance subjects: the host's subject follows the invitation; the signed-in subject follows the acceptance.
    [Fact] void should_keep_the_host_subject_from_the_host_outbox_to_the_ante_inbox() => OnboardingTrace.TheOne<UserInvitedToCreateTenant>(_trace.AnteInbox).Context.Subject.ToString().ShouldEqual(_hostSubject.ToString());
    [Fact] void should_record_the_receipt_under_the_host_subject() => OnboardingTrace.TheOne<CreateTenantInvitationReceived>(_trace.AnteLog).Context.Subject.ToString().ShouldEqual(_hostSubject.ToString());
    [Fact] void should_publish_the_token_under_the_host_subject() => OnboardingTrace.TheOne<InvitationTokenIssued>(_trace.AnteOutbox).Context.Subject.ToString().ShouldEqual(_hostSubject.ToString());
    [Fact] void should_deliver_the_token_under_the_host_subject() => OnboardingTrace.TheOne<InvitationTokenIssued>(_trace.HostReceipt).Context.Subject.ToString().ShouldEqual(_hostSubject.ToString());
    [Fact] void should_record_the_acceptance_under_the_signed_in_subject() => OnboardingTrace.TheOne<InvitationToCreateTenantAccepted>(_trace.AnteLog).Context.Subject.ToString().ShouldEqual(_subject);
    [Fact] void should_publish_the_acceptance_under_the_signed_in_subject() => OnboardingTrace.TheOne<InvitationToCreateTenantAccepted>(_trace.AnteOutbox).Context.Subject.ToString().ShouldEqual(_subject);
    [Fact] void should_deliver_the_acceptance_under_the_signed_in_subject() => OnboardingTrace.TheOne<InvitationToCreateTenantAccepted>(_trace.HostReceipt).Context.Subject.ToString().ShouldEqual(_subject);
    [Fact] void should_record_the_legal_acceptance_under_the_signed_in_subject() => OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.AnteLog).Context.Subject.ToString().ShouldEqual(_subject);
    [Fact] void should_publish_the_legal_acceptance_under_the_signed_in_subject() => OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.AnteOutbox).Context.Subject.ToString().ShouldEqual(_subject);
    [Fact] void should_deliver_the_legal_acceptance_under_the_signed_in_subject() => OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.HostReceipt).Context.Subject.ToString().ShouldEqual(_subject);

    // Occurrence: Ante's outbox keeps the time each fact was recorded. Chronicle stamps the host's inbox copy when it
    // forwards it, so the host receives a time no earlier than Ante's, not the same one.
    [Fact] void should_publish_the_acceptance_with_the_time_it_was_recorded() =>
        OnboardingTrace.TheOne<InvitationToCreateTenantAccepted>(_trace.AnteOutbox).Context.Occurred.ShouldEqual(OnboardingTrace.TheOne<InvitationToCreateTenantAccepted>(_trace.AnteLog).Context.Occurred);
    [Fact] void should_publish_the_legal_acceptance_with_the_time_it_was_recorded() =>
        OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.AnteOutbox).Context.Occurred.ShouldEqual(OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.AnteLog).Context.Occurred);
    [Fact] void should_deliver_the_acceptance_no_earlier_than_it_was_recorded() =>
        (OnboardingTrace.TheOne<InvitationToCreateTenantAccepted>(_trace.HostReceipt).Context.Occurred >= OnboardingTrace.TheOne<InvitationToCreateTenantAccepted>(_trace.AnteLog).Context.Occurred).ShouldBeTrue();
    [Fact] void should_deliver_the_legal_acceptance_no_earlier_than_it_was_recorded() =>
        (OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.HostReceipt).Context.Occurred >= OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.AnteLog).Context.Occurred).ShouldBeTrue();
}
