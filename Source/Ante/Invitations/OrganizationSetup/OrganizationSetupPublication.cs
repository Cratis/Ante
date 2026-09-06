// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Legal;
using Ante.Contracts.Organization;
using Ante.Outbox;
using MongoDB.Driver;

namespace Ante.Invitations.OrganizationSetup;

/// <summary>
/// Durable record of what organization setup - from an invitation or from self-service registration -
/// locally committed to Ante's own event log - the "Recorded" boundary from the Recorded/Published
/// distinction the onboarding adoption epic proposes. An instance exists only once setup has been
/// submitted; absence means setup never started (or the invitation is unknown), which tells the frontend
/// it is safe to (re)submit. <see cref="LegalRecorded"/> distinguishes whether a
/// <see cref="LegalTermsAccepted"/> fact was part of that same append, since nothing else durably
/// remembers that once the append itself has happened.
/// </summary>
/// <param name="Id">The invitation or registration identifier.</param>
/// <param name="OrganizationName">The name of the organization that was set up.</param>
/// <param name="LegalRecorded">Whether a <see cref="LegalTermsAccepted"/> fact was recorded alongside the acceptance.</param>
[ReadModel]
[FromEvent<InvitationToCreateTenantAccepted>]
[FromEvent<OrganizationRegistrationCompleted>]
[FromEvent<LegalTermsAccepted>]
public record OrganizationSetupProgress(
    InvitationId Id,
    [SetFrom<InvitationToCreateTenantAccepted>(nameof(InvitationToCreateTenantAccepted.TenantName))]
    [SetFrom<OrganizationRegistrationCompleted>(nameof(OrganizationRegistrationCompleted.TenantName))]
    TenantName OrganizationName,
    [SetValue<LegalTermsAccepted>(true)] bool LegalRecorded = false);

/// <summary>
/// Durable evidence that organization setup's public facts - from an invitation or from self-service
/// registration - have reached Ante's own outbox - the "Published" boundary. <see cref="AcceptancePublished"/>
/// is set once the acceptance/registration itself has reached the outbox; <see cref="LegalPublished"/>
/// mirrors the legal fact separately reaching the outbox, when one was recorded locally.
/// </summary>
/// <param name="Id">The invitation or registration identifier.</param>
/// <param name="AcceptancePublished">Whether the acceptance/registration itself has reached the outbox.</param>
/// <param name="LegalPublished">Whether a <see cref="LegalTermsAccepted"/> fact has reached the outbox.</param>
[ReadModel]
[EventSequence(EventSequenceId.OutboxId)]
[FromEvent<InvitationToCreateTenantAccepted>]
[FromEvent<OrganizationRegistrationCompleted>]
[FromEvent<LegalTermsAccepted>]
public record OrganizationSetupPublished(
    InvitationId Id,
    [SetValue<InvitationToCreateTenantAccepted>(true)]
    [SetValue<OrganizationRegistrationCompleted>(true)]
    bool AcceptancePublished = false,
    [SetValue<LegalTermsAccepted>(true)] bool LegalPublished = false);

/// <summary>
/// The pure decision of whether organization setup's acceptance/registration - including its legal fact,
/// when one was recorded - has fully reached Ante's outbox. Kept free of the Mongo round-trips so it can
/// be exercised directly from a spec.
/// </summary>
public static class OrganizationSetupPublication
{
    /// <summary>
    /// Determines whether every fact this setup recorded locally has also reached the outbox.
    /// </summary>
    /// <param name="recorded">The durable local record, or null when setup was never recorded.</param>
    /// <param name="published">The durable outbox record, or null when nothing has been published yet.</param>
    /// <returns>True when fully published; otherwise false.</returns>
    public static bool IsFullyPublished(OrganizationSetupProgress? recorded, OrganizationSetupPublished? published) =>
        recorded is not null &&
        published is { AcceptancePublished: true } &&
        (!recorded.LegalRecorded || published.LegalPublished);
}

/// <summary>
/// Accelerates the organization-setup flow's live status subscription - shared by invited tenant creation
/// and self-service registration - once its durable evidence confirms full publication.
/// </summary>
/// <param name="recordedCollection">The durable setup-record collection.</param>
/// <param name="publishedCollection">The durable outbox-publication collection.</param>
/// <param name="subscriptions">The subscription tracker.</param>
public class OrganizationPublicationStatusNotifier(
    IMongoCollection<OrganizationSetupProgress> recordedCollection,
    IMongoCollection<OrganizationSetupPublished> publishedCollection,
    OrganizationSetupStatusSubscriptions subscriptions) : IPublicationStatusNotifier
{
    /// <inheritdoc/>
    public async Task NotifyIfPublished(EventSourceId eventSourceId)
    {
        var invitationId = (InvitationId)Guid.Parse(eventSourceId.Value);
        var recorded = await recordedCollection.Find(Builders<OrganizationSetupProgress>.Filter.Eq(progress => progress.Id, invitationId)).FirstOrDefaultAsync();
        if (recorded is null)
        {
            // Not an organization setup or registration - most likely the shared
            // LegalTermsAcceptanceOutbox checking on behalf of a different flow's acceptance.
            return;
        }

        var published = await publishedCollection.Find(Builders<OrganizationSetupPublished>.Filter.Eq(progress => progress.Id, invitationId)).FirstOrDefaultAsync();
        if (OrganizationSetupPublication.IsFullyPublished(recorded, published))
        {
            subscriptions.MarkAccepted(invitationId, recorded.OrganizationName);
        }
    }
}
