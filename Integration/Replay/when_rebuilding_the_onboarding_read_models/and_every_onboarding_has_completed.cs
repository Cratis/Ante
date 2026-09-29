// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Ante.Integration.given;
using Ante.Integration.Replay.given;
using Ante.Invitations.OrganizationSetup;
using Ante.Invitations.Receiving;
using Ante.Invitations.UserSetup;
using Ante.Legal.Receiving;
using Ante.Organization.Names;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Integration.Replay.when_rebuilding_the_onboarding_read_models;

/// <summary>
/// Replaying every onboarding projection from the first event - what an operator does after an upgrade that changes a
/// read model's schema - rebuilds each read model into exactly the documents it held before, name claims released by the
/// host included. The kernel's constraint index, which a replay does not touch, still refuses every claimed name and
/// still frees every released one (Cratis/Ante#120).
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_every_onboarding_has_completed : an_onboarding_history
{
    static readonly IReadOnlyList<(Type ReadModel, Func<AnteApplication, Task<IReadOnlyList<string>>> Snapshot)> _readModels =
    [
        (typeof(OrganizationNameClaim), Snapshots.Of<OrganizationNameClaim>),
        (typeof(OrganizationSetupProgress), Snapshots.Of<OrganizationSetupProgress>),
        (typeof(OrganizationSetupPublished), Snapshots.Of<OrganizationSetupPublished>),
        (typeof(UserSetupProgress), Snapshots.Of<UserSetupProgress>),
        (typeof(JoinTenantAcceptancePublished), Snapshots.Of<JoinTenantAcceptancePublished>),
        (typeof(PendingInvitationToJoin), Snapshots.Of<PendingInvitationToJoin>),
        (typeof(PendingInvitationToCreateOrganization), Snapshots.Of<PendingInvitationToCreateOrganization>),
        (typeof(InvitationAwaitingSigningKey), Snapshots.Of<InvitationAwaitingSigningKey>),
        (typeof(ActivatedLegalDocumentSet), Snapshots.Of<ActivatedLegalDocumentSet>),
    ];

    readonly Dictionary<string, IReadOnlyList<string>> _before = new(StringComparer.Ordinal);
    readonly Dictionary<string, IReadOnlyList<string>> _after = new(StringComparer.Ordinal);
    readonly Dictionary<string, (ulong Handled, int Observed)> _replayed = new(StringComparer.Ordinal);
    readonly Dictionary<string, JsonDocument> _registeringClaimedNames = new(StringComparer.Ordinal);
    readonly Dictionary<string, AppendResult> _appendingClaimedNames = new(StringComparer.Ordinal);
    readonly Dictionary<string, JsonDocument> _registeringFreedNames = new(StringComparer.Ordinal);

    IEnumerable<string> ClaimedNames => [CreatedName, ReservedName, ReclaimedName];

    IEnumerable<string> FreedNames => [ReleasedName, RegisteredThenReleasedName];

    async Task Because()
    {
        var projections = new Dictionary<string, string>(StringComparer.Ordinal);
        await using (var scope = Ante.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
            foreach (var (readModel, _) in _readModels)
            {
                projections[readModel.Name] = store.Projections.GetProjectionIdForModel(readModel).Value;
            }
        }

        foreach (var (readModel, snapshot) in _readModels)
        {
            await KernelObservers.WaitUntilCaughtUp(Ante, projections[readModel.Name]);
            _before[readModel.Name] = await snapshot(Ante);
        }

        foreach (var (readModel, snapshot) in _readModels)
        {
            var observer = await KernelObservers.Replay(Ante, projections[readModel.Name]);
            _replayed[readModel.Name] = (observer.HandledEventCount, await KernelObservers.EventsObservedBy(Ante, observer));
            _after[readModel.Name] = await snapshot(Ante);
        }

        // Case-changed, so only the case-insensitive rule can refuse them.
        foreach (var name in ClaimedNames)
        {
            _registeringClaimedNames[name] = await TryRegister(Guid.NewGuid(), name.ToUpperInvariant());
        }

        // Straight to the event log: no validator, no handler and no read model stand between the claim and the kernel.
        await using (var scope = Ante.Services.CreateAsyncScope())
        {
            var eventLog = scope.ServiceProvider.GetRequiredService<IEventStore>().EventLog;
            foreach (var name in ClaimedNames)
            {
                _appendingClaimedNames[name] = await eventLog.Append($"probe-{Guid.NewGuid():N}", new OrganizationNameReservationReceived(name.ToLowerInvariant()));
            }
        }

        foreach (var name in FreedNames)
        {
            _registeringFreedNames[name] = await TryRegister(Guid.NewGuid(), name);
        }
    }

    [Fact] void should_rebuild_every_read_model() => _replayed.Keys.ShouldContainOnly(_readModels.Select(readModel => readModel.ReadModel.Name));
    [Fact] void should_redeliver_every_event_each_projection_observes() =>
        _replayed.Where(entry => entry.Value.Handled != (ulong)entry.Value.Observed)
            .Select(entry => $"{entry.Key}: handled {entry.Value.Handled} of {entry.Value.Observed}").ShouldBeEmpty();
    [Fact] void should_have_claimed_exactly_the_claimed_names_before_the_rebuild() => _before[nameof(OrganizationNameClaim)].Count.ShouldEqual(3);
    [Fact] void should_rebuild_the_name_claims_as_they_were() => Rebuilt<OrganizationNameClaim>();
    [Fact] void should_rebuild_the_organization_setup_progress_and_registration_owners_as_they_were() => Rebuilt<OrganizationSetupProgress>();
    [Fact] void should_rebuild_the_organization_setup_publication_as_it_was() => Rebuilt<OrganizationSetupPublished>();
    [Fact] void should_rebuild_the_user_setup_progress_as_it_was() => Rebuilt<UserSetupProgress>();
    [Fact] void should_rebuild_the_join_publication_as_it_was() => Rebuilt<JoinTenantAcceptancePublished>();
    [Fact] void should_rebuild_the_pending_join_invitations_as_they_were() => Rebuilt<PendingInvitationToJoin>();
    [Fact] void should_rebuild_the_pending_create_invitations_as_they_were() => Rebuilt<PendingInvitationToCreateOrganization>();
    [Fact] void should_rebuild_the_invitations_awaiting_a_signing_key_as_they_were() => Rebuilt<InvitationAwaitingSigningKey>();
    [Fact] void should_rebuild_the_activated_legal_documents_as_they_were() => Rebuilt<ActivatedLegalDocumentSet>();
    [Fact] void should_still_refuse_to_register_a_claimed_name() =>
        _registeringClaimedNames.Where(entry => IsSuccess(entry.Value)).Select(entry => entry.Key).ShouldBeEmpty();
    [Fact] void should_still_refuse_a_claimed_name_at_the_event_store() =>
        _appendingClaimedNames.Where(entry => !entry.Value.ConstraintViolations.Any(violation => violation.ConstraintName.Value == OrganizationSetupConstraintNames.UniqueOrganizationName))
            .Select(entry => entry.Key).ShouldBeEmpty();
    [Fact] void should_still_accept_a_released_name() =>
        _registeringFreedNames.Where(entry => !IsSuccess(entry.Value)).Select(entry => $"{entry.Key}: {entry.Value.RootElement}").ShouldBeEmpty();

    void Rebuilt<TReadModel>() =>
        Snapshots.Text(_after[typeof(TReadModel).Name]).ShouldEqual(Snapshots.Text(_before[typeof(TReadModel).Name]));
}
