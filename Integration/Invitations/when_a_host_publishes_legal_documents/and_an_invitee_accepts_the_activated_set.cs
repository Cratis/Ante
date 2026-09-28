// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Ante.Contracts.Legal;
using Ante.Integration.given;
using Ante.Invitations;
using Ante.Invitations.Receiving;
using Ante.Invitations.UserSetup;
using Ante.Legal;
using Ante.Legal.Receiving;
using Ante.Resources;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Integration.Invitations.when_a_host_publishes_legal_documents;

[Collection(ChronicleCollection.Name)]
public class and_an_invitee_accepts_the_activated_set : a_running_ante
{
    readonly Guid _invitationId = NewInvitationId();
    readonly string _subject = $"user-{Guid.NewGuid():N}";
    JsonDocument _beforePublication;
    JsonDocument _afterPublication;
    LegalDocumentSetActivated _activation;
    LegalTermsAccepted _accepted;
    LegalDocumentSetRejected _rejection;
    JsonDocument _current;
    JsonDocument _raceResult;
    bool _raceTriggered;
    int _raceFactsCount;

    protected override bool UseLegalInbox => true;

    protected override Func<IServiceProvider, ILegalDocumentSource>? LegalDocumentFactory => services =>
        new activating_during_acceptance(
            ActivatorUtilities.CreateInstance<InboxLegalDocumentSource>(services),
            async () =>
            {
                _raceTriggered = true;
                await Host.Publish(LegalDocumentSetId, new LegalDocumentSetPublished(3, "2026-03", "Terms three", "Privacy three"));
                var store = services.GetRequiredService<IEventStore>();
                await Eventually.Until(async () =>
                    (await store.EventLog.GetForEventSourceIdAndEventTypes(LegalDocumentSetId,
                        [typeof(LegalDocumentSetReceived).GetEventType()]))
                        .Select(entry => entry.Content).OfType<LegalDocumentSetReceived>().LastOrDefault()?.Revision.Value == 3,
                    what: "third legal revision activation before command append");
            },
            () => _raceEnabled);

    bool _raceEnabled;

    async Task Because()
    {
        var issued = await Invite(Host, _invitationId, JoinInvitation());
        using var exchange = await Ante.ExchangeInvitation(issued.Token, _subject);
        _beforePublication = await Ante.Execute("/api/invitations/user-setup",
            new { invitationId = _invitationId, firstName = "Jane", lastName = "Doe", acceptedLegalTerms = false, acceptedLegalVersion = "" },
            _subject);

        await Host.Publish(LegalDocumentSetId, new LegalDocumentSetPublished(2, "2026-02", "Terms two", "Privacy two"));
        _activation = await Host.WaitForFromAnte<LegalDocumentSetActivated>(LegalDocumentSetId);
        await Host.Publish(LegalDocumentSetId, new LegalDocumentSetPublished(1, "2026-01", "Old terms", "Old privacy"));
        await Host.Publish(LegalDocumentSetId, new LegalDocumentSetPublished(2, "2026-02", "Conflicting terms", "Privacy two"));

        var client = Ante.Services.GetRequiredService<IChronicleClient>();
        var store = await client.GetEventStore(AnteStoreName);
        _rejection = await Eventually.Get(async () =>
            (await store.EventLog.GetForEventSourceIdAndEventTypes(LegalDocumentSetId,
                [typeof(LegalDocumentSetRejected).GetEventType()]))
                .Select(entry => entry.Content).OfType<LegalDocumentSetRejected>().FirstOrDefault(),
            what: "conflicting legal revision quarantine");

        using var response = await Ante.CreateClient().GetAsync("/api/legal/current");
        _current = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        _afterPublication = await ExecuteOnceProjected("/api/invitations/user-setup",
            new { invitationId = _invitationId, firstName = "Jane", lastName = "Doe", acceptedLegalTerms = true, acceptedLegalVersion = "2026-02" },
            _subject);
        _accepted = await Host.WaitForFromAnte<LegalTermsAccepted>(_invitationId.ToString());

        var otherInvitation = NewInvitationId();
        var otherSubject = $"user-{Guid.NewGuid():N}";
        var otherIssued = await Invite(Host, otherInvitation, JoinInvitation());
        using var otherExchange = await Ante.ExchangeInvitation(otherIssued.Token, otherSubject);
        await Eventually.Until(async () =>
            await store.ReadModels.GetInstanceById<PendingInvitationToJoin>(otherInvitation) is not null,
            what: "second invitation projection before racing the command");

        _raceEnabled = true;
        _raceResult = await Ante.Execute("/api/invitations/user-setup",
            new { invitationId = otherInvitation, firstName = "Jane", lastName = "Doe", acceptedLegalTerms = true, acceptedLegalVersion = "2026-02" },
            otherSubject);
        var raceFacts = await store.EventLog.GetForEventSourceIdAndEventTypes(otherInvitation.ToString("D"),
            [typeof(OnboardingAttemptClaimed).GetEventType(), typeof(InvitationToJoinTenantAccepted).GetEventType(), typeof(LegalTermsAccepted).GetEventType()]);
        _raceFactsCount = raceFacts.Count;
    }

    [Fact] void should_block_before_first_activation() => IsSuccess(_beforePublication).ShouldBeFalse();
    [Fact] void should_acknowledge_the_activated_revision() => _activation.Revision.Value.ShouldEqual(2);
    [Fact] void should_quarantine_a_conflicting_revision() => _rejection.Reason.ShouldEqual(LegalDocumentRejectionReason.ConflictingRevisionOrVersion);
    [Fact] void should_not_roll_back_the_current_version() => _current.RootElement.GetProperty("data").GetProperty("version").GetString().ShouldEqual("2026-02");
    [Fact] void should_accept_the_activated_documents() => IsSuccess(_afterPublication).ShouldBeTrue();
    [Fact] void should_publish_the_accepted_version() => _accepted.Version.Value.ShouldEqual("2026-02");
    [Fact] void should_exercise_activation_during_command_execution() => _raceTriggered.ShouldBeTrue();
    [Fact] void should_reject_the_command_when_activation_races_its_append() => IsSuccess(_raceResult).ShouldBeFalse();
    [Fact] void should_report_the_race_as_a_concurrency_violation() => FirstValidationResult(_raceResult).GetProperty("reason").GetString().ShouldEqual("concurrencyViolation");
    [Fact] void should_show_the_localized_retry_message() => FirstValidationResult(_raceResult).GetProperty("message").GetString().ShouldEqual(Messages.Get("ConcurrentChange"));

    static JsonElement FirstValidationResult(JsonDocument result) => result.RootElement.GetProperty("validationResults")[0];
    [Fact] void should_not_append_onboarding_or_acceptance_facts_for_the_rejected_command() => _raceFactsCount.ShouldEqual(0);

    class activating_during_acceptance(
        InboxLegalDocumentSource inner,
        Func<Task> activateNext,
        Func<bool> isArmed) : IActivatedLegalDocumentSource, ILegalDocumentAvailability
    {
        bool _activated;
        public bool RequiresDocuments => inner.RequiresDocuments;
        public Task<LegalDocumentSet?> GetCurrent() => inner.GetCurrent();

        public async Task<ActivatedLegalSnapshot> GetActivated()
        {
            var snapshot = await inner.GetActivated();
            if (isArmed() && !_activated)
            {
                _activated = true;
                await activateNext();
            }
            return snapshot;
        }
    }
}
