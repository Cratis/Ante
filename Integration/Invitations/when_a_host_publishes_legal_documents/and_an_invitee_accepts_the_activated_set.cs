// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Ante.Contracts.Legal;
using Ante.Integration.given;
using Ante.Legal;
using Ante.Legal.Receiving;
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
    bool _staleAcceptanceRejected;

    protected override bool UseLegalInbox => true;

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

        // Resolve the same evidence and append envelope as an onboarding command. Advance the
        // legal stream after resolution but before committing that envelope.
        await using var scope = Ante.Services.CreateAsyncScope();
        var scopedStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
        var source = scope.ServiceProvider.GetRequiredService<ILegalDocumentSource>();
        var resolved = await LegalAcceptanceEvidence.ResolveWithScope(
            source, true, "2026-02", "Acme", AnteApplication.IdentityProvider, _subject);
        Assert.True(resolved.TryGetResult(out var evidence));
        var otherInvitation = NewInvitationId().ToString();
        var commandAppend = await LegalAcceptanceEvidence.ForAppend(scopedStore, otherInvitation, evidence.Events, evidence);
        await Host.Publish(LegalDocumentSetId, new LegalDocumentSetPublished(3, "2026-03", "Terms three", "Privacy three"));
        await Eventually.Until(async () =>
            (await store.EventLog.GetForEventSourceIdAndEventTypes(LegalDocumentSetId,
                [typeof(LegalDocumentSetReceived).GetEventType()]))
                .Select(entry => entry.Content).OfType<LegalDocumentSetReceived>().LastOrDefault()?.Revision.Value == 3,
            what: "third legal revision activation");
        var staleAppend = await scopedStore.EventLog.AppendMany(commandAppend.Events, concurrencyScopes: commandAppend.ConcurrencyScopes.ToDictionary());
        _staleAcceptanceRejected = staleAppend.HasConcurrencyViolations;
    }

    [Fact] void should_block_before_first_activation() => IsSuccess(_beforePublication).ShouldBeFalse();
    [Fact] void should_acknowledge_the_activated_revision() => _activation.Revision.Value.ShouldEqual(2);
    [Fact] void should_quarantine_a_conflicting_revision() => _rejection.Reason.ShouldEqual(LegalDocumentRejectionReason.ConflictingRevisionOrVersion);
    [Fact] void should_not_roll_back_the_current_version() => _current.RootElement.GetProperty("data").GetProperty("version").GetString().ShouldEqual("2026-02");
    [Fact] void should_accept_the_activated_documents() => IsSuccess(_afterPublication).ShouldBeTrue();
    [Fact] void should_publish_the_accepted_version() => _accepted.Version.Value.ShouldEqual("2026-02");
    [Fact] void should_reject_an_acceptance_when_activation_races_its_append() => _staleAcceptanceRejected.ShouldBeTrue();
}
