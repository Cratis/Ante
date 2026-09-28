// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Ante.Contracts.Legal;
using Ante.Integration.given;
using Ante.Invitations;
using Ante.Invitations.Accepting;
using Ante.Invitations.Receiving;
using Ante.Invitations.UserSetup;
using Ante.Legal;
using Ante.Resources;
using Cratis.Chronicle.EventSequences.Concurrency;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Integration.Invitations.when_a_host_publishes_legal_documents;

[Collection(ChronicleCollection.Name)]
public class and_revocation_races_an_invitee_acceptance : a_running_ante
{
    readonly Guid _invitationId = NewInvitationId();
    readonly string _subject = $"user-{Guid.NewGuid():N}";
    JsonDocument _result;
    bool _revokedDuringFence;
    int _acceptanceFactsCount;

    protected override bool UseLegalInbox => true;

    protected override Func<IServiceProvider, IInvitationAcceptanceFence>? AcceptanceFenceFactory => services =>
        new revoking_after_reading_fence(
            ActivatorUtilities.CreateInstance<InvitationAcceptanceFence>(services),
            async id =>
            {
                var client = services.GetRequiredService<IChronicleClient>();
                var store = await client.GetEventStore(AnteStoreName);
                var append = await store.EventLog.Append(id.Value.ToString("D"), new InvitationRevocationReceived());
                if (!append.IsSuccess)
                {
                    throw new InvalidOperationException("Could not append the competing revocation.");
                }
                _revokedDuringFence = true;
            });

    async Task Because()
    {
        await Host.Publish(LegalDocumentSetId, new LegalDocumentSetPublished(1, "2026-01", "Terms", "Privacy"));
        await Host.WaitForFromAnte<LegalDocumentSetActivated>(LegalDocumentSetId);

        var issued = await Invite(Host, _invitationId, JoinInvitation());
        using var exchange = await Ante.ExchangeInvitation(issued.Token, _subject);
        var client = Ante.Services.GetRequiredService<IChronicleClient>();
        var store = await client.GetEventStore(AnteStoreName);
        await Eventually.Until(async () =>
            await store.ReadModels.GetInstanceById<PendingInvitationToJoin>(_invitationId) is not null,
            what: "invitation projection before racing revocation");

        _result = await Ante.Execute("/api/invitations/user-setup",
            new { invitationId = _invitationId, firstName = "Jane", lastName = "Doe", acceptedLegalTerms = true, acceptedLegalVersion = "2026-01" },
            _subject);
        var facts = await store.EventLog.GetForEventSourceIdAndEventTypes(_invitationId.ToString("D"),
            [typeof(OnboardingAttemptClaimed).GetEventType(), typeof(InvitationToJoinTenantAccepted).GetEventType(), typeof(LegalTermsAccepted).GetEventType()]);
        _acceptanceFactsCount = facts.Count;
    }

    [Fact] void should_revoke_after_the_fence_is_read() => _revokedDuringFence.ShouldBeTrue();
    [Fact] void should_reject_the_acceptance() => IsSuccess(_result).ShouldBeFalse();
    [Fact] void should_report_a_concurrency_violation() => _result.RootElement.GetProperty("validationResults")[0].GetProperty("reason").GetString().ShouldEqual("concurrencyViolation");
    [Fact] void should_localize_the_retry_message() => _result.RootElement.GetProperty("validationResults")[0].GetProperty("message").GetString().ShouldEqual(Messages.Get("ConcurrentChange"));
    [Fact] void should_not_append_any_acceptance_fact() => _acceptanceFactsCount.ShouldEqual(0);

    class revoking_after_reading_fence(InvitationAcceptanceFence inner, Func<InvitationId, Task> revoke) : IInvitationAcceptanceFence
    {
        public async Task<ConcurrencyScope?> For(InvitationId invitationId, InvitationFlowType expectedFlow)
        {
            var scope = await inner.For(invitationId, expectedFlow);
            if (scope is not null)
            {
                await revoke(invitationId);
            }
            return scope;
        }
    }
}
