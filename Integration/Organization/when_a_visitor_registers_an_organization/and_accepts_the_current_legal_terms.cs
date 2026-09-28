// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Ante.Contracts.Legal;
using Ante.Contracts.Organization;
using Ante.Integration.given;
using Ante.Invitations;
using Ante.Invitations.OrganizationSetup;
using Ante.Legal;
using Ante.Organization.Registration;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Integration.Organization.when_a_visitor_registers_an_organization;

[Collection(ChronicleCollection.Name)]
public class and_accepts_the_current_legal_terms : a_running_ante
{
    readonly Guid _registrationId = Guid.NewGuid();
    readonly string _subject = $"visitor-{Guid.NewGuid():N}";
    readonly string _email = $"{Guid.NewGuid():N}@example.com";
    readonly string _organization = $"Self-{Guid.NewGuid():N}"[..20];
    JsonDocument _result;
    OrganizationRegistrationCompleted _completed;
    LegalTermsAccepted _legal;
    RegistrationOwnerRecorded _owner;
    bool _eventsHaveExpectedSubjects;
    OrganizationSetupProgress _progress;
    JsonDocument _ownerStatus;
    JsonDocument _strangerStatus;

    protected override ILegalDocumentSource? LegalDocuments => new CurrentLegalDocuments();

    async Task Because()
    {
        var started = await Ante.Execute(
            "/api/organization/registration/start", new { registrationId = _registrationId }, _subject, _email);
        if (!IsSuccess(started))
        {
            throw new InvalidOperationException($"Registration start did not succeed: {started.RootElement}");
        }

        _result = await ExecuteOnceProjected(
            "/api/organization/registration",
            new { registrationId = _registrationId, organizationName = _organization, firstName = "Grace", lastName = "Hopper", acceptedLegalTerms = true, acceptedLegalVersion = CurrentLegalDocuments.Version.Value },
            _subject,
            _email);
        _completed = await Host.WaitForFromAnte<OrganizationRegistrationCompleted>(_registrationId.ToString());
        _legal = await Host.WaitForFromAnte<LegalTermsAccepted>(_registrationId.ToString());
        _owner = await Eventually.Get(async () =>
        {
            await using var scope = Ante.Services.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
            var entries = await store.EventLog.GetForEventSourceIdAndEventTypes(
                _registrationId.ToString("D"),
                [typeof(RegistrationOwnerRecorded).GetEventType()]);
            return entries.Select(entry => entry.Content).OfType<RegistrationOwnerRecorded>().FirstOrDefault();
        },
        what: "persisted registration owner event");
        await using (var scope = Ante.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
            var entries = await store.EventLog.GetForEventSourceIdAndEventTypes(
                _registrationId.ToString("D"),
                [typeof(OnboardingAttemptClaimed).GetEventType(), typeof(OrganizationRegistrationCompleted).GetEventType(),
                    typeof(RegistrationOwnerRecorded).GetEventType(), typeof(LegalTermsAccepted).GetEventType()]);
            _eventsHaveExpectedSubjects = entries.Count == 4 && entries.All(entry => entry.Content switch
            {
                RegistrationOwnerRecorded => !entry.Context.SubjectIsEventSourceId && entry.Context.Subject.ToString() == _subject,
                OnboardingAttemptClaimed or OrganizationRegistrationCompleted or LegalTermsAccepted =>
                    entry.Context.SubjectIsEventSourceId && entry.Context.Subject.ToString() == _registrationId.ToString("D"),
                _ => false,
            });
        }
        _progress = await Eventually.Get<OrganizationSetupProgress>(async () =>
        {
            await using var scope = Ante.Services.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
            var progress = await store.ReadModels.GetInstanceById<OrganizationSetupProgress>(_registrationId);
            return progress?.OwnerSubject?.Value == _subject ? progress : null;
        },
        what: "decrypted registration owner read model");
        _ownerStatus = await Ante.RegistrationStatus(_registrationId, _subject);
        _strangerStatus = await Ante.RegistrationStatus(_registrationId, $"stranger-{Guid.NewGuid():N}");
    }

    [Fact] void should_accept_the_command() => IsSuccess(_result).ShouldBeTrue();
    [Fact] void should_publish_the_organization_name() => _completed.TenantName.Value.ShouldEqual(_organization);
    [Fact] void should_publish_the_registering_subject() => _completed.Subject.ShouldEqual(_subject);

    // The owner's [PII] crosses Ante's store and the host inbox and must read back decrypted.
    [Fact] void should_deliver_the_owner_name_decrypted() => $"{_completed.FirstName.Value} {_completed.LastName.Value}".ShouldEqual("Grace Hopper");
    [Fact] void should_deliver_the_signed_in_email_decrypted() => _completed.Email.Value.ShouldEqual(_email);
    [Fact] void should_publish_legal_acceptance() => _legal.Version.ShouldEqual(CurrentLegalDocuments.Version);
    [Fact] void should_persist_the_owner_event() => _owner.OwnerProvider.Value.ShouldEqual(AnteApplication.IdentityProvider);
    [Fact] void should_use_the_registration_id_for_registration_facts_and_owner_subject_for_owner_fact() => _eventsHaveExpectedSubjects.ShouldBeTrue();
    [Fact] void should_release_the_projected_owner_subject() => _progress.OwnerSubject!.Value.ShouldEqual(_subject);
    [Fact] void should_return_status_to_the_recorded_owner() => _ownerStatus.RootElement.GetProperty("data").GetProperty("organizationName").GetString().ShouldEqual(_organization);
    [Fact] void should_hide_the_registration_from_another_identity() => _strangerStatus.RootElement.GetProperty("data").GetProperty("organizationName").GetString().ShouldEqual(string.Empty);
}
