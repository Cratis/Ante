// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Legal;
using Ante.Contracts.Organization;
using Ante.Integration.given;
using Ante.Invitations;
using Ante.Legal;
using Ante.Organization.Registration;
using Ante.Organization.Registration.Start;

namespace Ante.Integration.Organization.when_registering_end_to_end;

/// <summary>
/// One self-service registration, followed from Ante's event log to the host's receipt with every hop read back
/// through Chronicle. There is no host outbox or Ante inbox on this journey: the visitor starts it, not the host.
/// </summary>
[Collection(ChronicleCollection.Name)]
public class from_ante_log_to_host_receipt : a_running_ante
{
    readonly Guid _registrationId = Guid.NewGuid();
    readonly Guid _startCorrelation = Guid.NewGuid();
    readonly Guid _submissionCorrelation = Guid.NewGuid();
    readonly string _subject = $"visitor-{Guid.NewGuid():N}";
    readonly string _email = $"{Guid.NewGuid():N}@example.com";
    readonly string _organization = $"Self-{Guid.NewGuid():N}"[..20];
    OnboardingTrace _trace;

    protected override ILegalDocumentSource? LegalDocuments => new CurrentLegalDocuments();

    protected override IReadOnlyList<string>? RegistrationContextKeys => ["offer", "utm_source"];

    string Id => _registrationId.ToString("D");

    OrganizationRegistrationCompleted ExpectedRegistration =>
        new(_organization, _subject, AnteApplication.IdentityProvider, "Grace", "Brewster", "Hopper", _email, [new("offer", "trial"), new("utm_source", "site")]);

    LegalTermsAccepted ExpectedLegal => new(_organization, AnteApplication.IdentityProvider, _subject, CurrentLegalDocuments.Version);

    async Task Because()
    {
        var started = await Ante.Execute("/api/organization/registration/start", new { registrationId = _registrationId }, _subject, _email, _startCorrelation);
        if (!IsSuccess(started))
        {
            throw new InvalidOperationException($"Registration start did not succeed: {started.RootElement}");
        }

        await ExecuteOnceProjected(
            "/api/organization/registration",
            new
            {
                registrationId = _registrationId,
                organizationName = _organization,
                firstName = "Grace",
                middleName = "Brewster",
                lastName = "Hopper",
                acceptedLegalTerms = true,
                acceptedLegalVersion = CurrentLegalDocuments.Version.Value,
                signupContext = new[] { new { key = "offer", value = "trial" }, new { key = "utm_source", value = "site" }, new { key = "not_allowed", value = "dropped" } },
            },
            _subject,
            _email,
            _submissionCorrelation);
        _trace = await OnboardingTrace.CaptureSettled(Ante, Host, Id, trace => trace.HostReceipt.Count >= 2);
    }

    // Counts per hop: the visitor started this, so the host has published nothing and Ante received nothing.
    [Fact] void should_not_have_a_host_invitation() => OnboardingTrace.Types(_trace.HostOutbox).ShouldEqual(string.Empty);
    [Fact] void should_not_have_received_an_invitation() => OnboardingTrace.Types(_trace.AnteInbox).ShouldEqual(string.Empty);
    [Fact] void should_record_each_registration_fact_once_in_the_ante_log() =>
        OnboardingTrace.Types(_trace.AnteLog).ShouldEqual(string.Join(", ", new[]
        {
            nameof(LegalTermsAccepted), nameof(OnboardingAttemptClaimed), nameof(OrganizationRegistrationCompleted),
            nameof(RegistrationOwnerRecorded), nameof(RegistrationQuotaConsumed), nameof(RegistrationStarted),
        }));
    [Fact] void should_publish_only_the_registration_and_the_legal_acceptance_once_from_ante() =>
        OnboardingTrace.Types(_trace.AnteOutbox).ShouldEqual(string.Join(", ", nameof(LegalTermsAccepted), nameof(OrganizationRegistrationCompleted)));
    [Fact] void should_deliver_exactly_what_ante_published_to_the_host() =>
        OnboardingTrace.Types(_trace.HostReceipt).ShouldEqual(OnboardingTrace.Types(_trace.AnteOutbox));

    // Event source ids: the registration id, at every hop; only the per-identity quota counter lives elsewhere.
    [Fact] void should_publish_and_deliver_under_the_registration_id() =>
        _trace.AnteOutbox.Concat(_trace.HostReceipt).All(appended => appended.Context.EventSourceId.Value == Id).ShouldBeTrue();
    [Fact] void should_record_every_registration_fact_under_the_registration_id() =>
        _trace.AnteLog.Where(appended => appended.Content is not RegistrationQuotaConsumed).All(appended => appended.Context.EventSourceId.Value == Id).ShouldBeTrue();
    [Fact] void should_count_the_quota_under_a_different_event_source() =>
        OnboardingTrace.TheOne<RegistrationQuotaConsumed>(_trace.AnteLog).Context.EventSourceId.Value.ShouldNotEqual(Id);

    // Payloads: every property, identical at the log, the outbox and the host.
    [Fact] void should_record_the_complete_registration_in_the_ante_log() =>
        OnboardingTrace.Flatten(OnboardingTrace.TheOne<OrganizationRegistrationCompleted>(_trace.AnteLog).Content).ShouldEqual(OnboardingTrace.Flatten(ExpectedRegistration));
    [Fact] void should_publish_the_complete_registration_from_ante() =>
        OnboardingTrace.Flatten(OnboardingTrace.TheOne<OrganizationRegistrationCompleted>(_trace.AnteOutbox).Content).ShouldEqual(OnboardingTrace.Flatten(ExpectedRegistration));
    [Fact] void should_deliver_the_complete_registration_to_the_host() =>
        OnboardingTrace.Flatten(OnboardingTrace.TheOne<OrganizationRegistrationCompleted>(_trace.HostReceipt).Content).ShouldEqual(OnboardingTrace.Flatten(ExpectedRegistration));
    [Fact] void should_record_the_complete_legal_acceptance_in_the_ante_log() =>
        OnboardingTrace.Flatten(OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.AnteLog).Content).ShouldEqual(OnboardingTrace.Flatten(ExpectedLegal));
    [Fact] void should_publish_the_complete_legal_acceptance_from_ante() =>
        OnboardingTrace.Flatten(OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.AnteOutbox).Content).ShouldEqual(OnboardingTrace.Flatten(ExpectedLegal));
    [Fact] void should_deliver_the_complete_legal_acceptance_to_the_host() =>
        OnboardingTrace.Flatten(OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.HostReceipt).Content).ShouldEqual(OnboardingTrace.Flatten(ExpectedLegal));

    // Correlation ids: the start keeps its own; the submission's runs from the log to the host.
    [Fact] void should_record_the_start_under_its_command_correlation_id() => OnboardingTrace.TheOne<RegistrationStarted>(_trace.AnteLog).Context.CorrelationId.Value.ShouldEqual(_startCorrelation);
    [Fact] void should_record_the_registration_under_its_command_correlation_id() => OnboardingTrace.TheOne<OrganizationRegistrationCompleted>(_trace.AnteLog).Context.CorrelationId.Value.ShouldEqual(_submissionCorrelation);
    [Fact] void should_publish_the_registration_under_the_same_correlation_id() => OnboardingTrace.TheOne<OrganizationRegistrationCompleted>(_trace.AnteOutbox).Context.CorrelationId.Value.ShouldEqual(_submissionCorrelation);
    [Fact] void should_deliver_the_registration_under_the_same_correlation_id() => OnboardingTrace.TheOne<OrganizationRegistrationCompleted>(_trace.HostReceipt).Context.CorrelationId.Value.ShouldEqual(_submissionCorrelation);
    [Fact] void should_record_the_legal_acceptance_under_the_same_correlation_id() => OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.AnteLog).Context.CorrelationId.Value.ShouldEqual(_submissionCorrelation);
    [Fact] void should_publish_the_legal_acceptance_under_the_same_correlation_id() => OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.AnteOutbox).Context.CorrelationId.Value.ShouldEqual(_submissionCorrelation);
    [Fact] void should_deliver_the_legal_acceptance_under_the_same_correlation_id() => OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.HostReceipt).Context.CorrelationId.Value.ShouldEqual(_submissionCorrelation);

    // Compliance subjects: the registration facts are filed under the registration id; only the owner facts carry
    // the visitor's own subject.
    [Fact] void should_record_the_registration_under_the_registration_id_as_subject() => OnboardingTrace.TheOne<OrganizationRegistrationCompleted>(_trace.AnteLog).Context.Subject.ToString().ShouldEqual(Id);
    [Fact] void should_publish_the_registration_under_the_registration_id_as_subject() => OnboardingTrace.TheOne<OrganizationRegistrationCompleted>(_trace.AnteOutbox).Context.Subject.ToString().ShouldEqual(Id);
    [Fact] void should_deliver_the_registration_under_the_registration_id_as_subject() => OnboardingTrace.TheOne<OrganizationRegistrationCompleted>(_trace.HostReceipt).Context.Subject.ToString().ShouldEqual(Id);
    [Fact] void should_record_the_legal_acceptance_under_the_registration_id_as_subject() => OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.AnteLog).Context.Subject.ToString().ShouldEqual(Id);
    [Fact] void should_publish_the_legal_acceptance_under_the_registration_id_as_subject() => OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.AnteOutbox).Context.Subject.ToString().ShouldEqual(Id);
    [Fact] void should_deliver_the_legal_acceptance_under_the_registration_id_as_subject() => OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.HostReceipt).Context.Subject.ToString().ShouldEqual(Id);
    [Fact] void should_record_the_start_under_the_visitors_subject() => OnboardingTrace.TheOne<RegistrationStarted>(_trace.AnteLog).Context.Subject.ToString().ShouldEqual(_subject);
    [Fact] void should_record_the_owner_under_the_visitors_subject() => OnboardingTrace.TheOne<RegistrationOwnerRecorded>(_trace.AnteLog).Context.Subject.ToString().ShouldEqual(_subject);

    // Occurrence: Ante's outbox keeps the time each fact was recorded. Chronicle stamps the host's inbox copy when it
    // forwards it, so the host receives a time no earlier than Ante's, not the same one.
    [Fact] void should_publish_the_registration_with_the_time_it_was_recorded() =>
        OnboardingTrace.TheOne<OrganizationRegistrationCompleted>(_trace.AnteOutbox).Context.Occurred.ShouldEqual(OnboardingTrace.TheOne<OrganizationRegistrationCompleted>(_trace.AnteLog).Context.Occurred);
    [Fact] void should_publish_the_legal_acceptance_with_the_time_it_was_recorded() =>
        OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.AnteOutbox).Context.Occurred.ShouldEqual(OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.AnteLog).Context.Occurred);
    [Fact] void should_deliver_the_registration_no_earlier_than_it_was_recorded() =>
        (OnboardingTrace.TheOne<OrganizationRegistrationCompleted>(_trace.HostReceipt).Context.Occurred >= OnboardingTrace.TheOne<OrganizationRegistrationCompleted>(_trace.AnteLog).Context.Occurred).ShouldBeTrue();
    [Fact] void should_deliver_the_legal_acceptance_no_earlier_than_it_was_recorded() =>
        (OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.HostReceipt).Context.Occurred >= OnboardingTrace.TheOne<LegalTermsAccepted>(_trace.AnteLog).Context.Occurred).ShouldBeTrue();
}
