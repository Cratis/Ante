// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Legal;
using Ante.Contracts.Organization;
using Ante.Organization.Registration;
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
/// <remarks>
/// Pinned to the local event log: most of its events are declared in <c language="csharp">Cratis.Ante.Contracts</c>,
/// whose assembly-level <c language="csharp">[EventStore("Ante")]</c> would otherwise make Chronicle read it from
/// <c language="csharp">inbox-Ante</c> whenever <see cref="AnteOptions.EventStore"/> is not the literal "Ante".
/// <para>
/// The owner is carried by init properties, not optional constructor parameters: Chronicle's schema generator
/// drops the type and the <c language="csharp">[PII]</c> classification of a nullable concept parameter defaulted to
/// <see langword="null"/>, and the kernel then leaves the untyped values out of every instance it returns.
/// </para>
/// </remarks>
[ReadModel]
[EventLog]
[FromEvent<InvitationToCreateTenantAccepted>]
[FromEvent<OrganizationRegistrationCompleted>]
[FromEvent<RegistrationOwnerRecorded>]
[FromEvent<LegalTermsAccepted>]
public record OrganizationSetupProgress(
    InvitationId Id,
    [SetFrom<InvitationToCreateTenantAccepted>(nameof(InvitationToCreateTenantAccepted.TenantName))]
    [SetFrom<OrganizationRegistrationCompleted>(nameof(OrganizationRegistrationCompleted.TenantName))]
    TenantName OrganizationName,
    [SetValue<LegalTermsAccepted>(true)] bool LegalRecorded = false)
{
    /// <summary>
    /// Gets the self-service owner's subject, when recorded.
    /// </summary>
    [Subject]
    public RegistrationOwnerSubject? OwnerSubject { get; init; }

    /// <summary>
    /// Gets the self-service owner's identity provider, when recorded.
    /// </summary>
    public IdentityProviderName? OwnerProvider { get; init; }
}

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
/// <param name="eventStore">The authoritative local log and outbox, for projection-lag recovery.</param>
public class OrganizationPublicationStatusNotifier(
    IMongoCollection<OrganizationSetupProgress> recordedCollection,
    IMongoCollection<OrganizationSetupPublished> publishedCollection,
    OrganizationSetupStatusSubscriptions subscriptions,
    IEventStore eventStore) : IPublicationStatusNotifier
{
    /// <inheritdoc/>
    public async Task NotifyIfPublished(EventSourceId eventSourceId)
    {
        var invitationId = (InvitationId)Guid.Parse(eventSourceId.Value);
        var recorded = await recordedCollection.Find(Builders<OrganizationSetupProgress>.Filter.Eq(progress => progress.Id, invitationId)).FirstOrDefaultAsync();
        var published = await publishedCollection.Find(Builders<OrganizationSetupPublished>.Filter.Eq(progress => progress.Id, invitationId)).FirstOrDefaultAsync();
        if (recorded is not null && OrganizationSetupPublication.IsFullyPublished(recorded, published))
        {
            subscriptions.MarkAccepted(invitationId, recorded.OrganizationName);
            return;
        }

        // Publication can complete before either Mongo projection catches up. The local log identifies
        // whether this is an invited setup or a self-registration and supplies its organization name;
        // only the matching public fact and every locally recorded legal fact may unlock the stream.
        // Matching is by event type id because the published copy may carry a different generation.
        EventType[] relevantTypes =
        [
            typeof(InvitationToCreateTenantAccepted).GetEventType(),
            typeof(OrganizationRegistrationCompleted).GetEventType(),
            typeof(LegalTermsAccepted).GetEventType(),
        ];
        var local = await eventStore.EventLog.GetForEventSourceIdAndEventTypes(eventSourceId, relevantTypes);
        var accepted = local.Where(entry => entry.Context.EventType.Id == relevantTypes[0].Id ||
            entry.Context.EventType.Id == relevantTypes[1].Id).ToArray();
        if (accepted.Length != 1)
        {
            // Not this flow, or ambiguous acceptance from two flows sharing one event source.
            return;
        }

        var organizationName = accepted[0].Content switch
        {
            InvitationToCreateTenantAccepted invited => invited.TenantName,
            OrganizationRegistrationCompleted registered => registered.TenantName,
            _ => null,
        };
        if (organizationName is null)
        {
            return;
        }

        var outbox = await eventStore.GetEventSequence(EventSequenceId.Outbox).GetForEventSourceIdAndEventTypes(eventSourceId, relevantTypes);
        if (outbox.Any(entry => entry.Context.EventType.Id == accepted[0].Context.EventType.Id) &&
            (!local.Any(entry => entry.Context.EventType.Id == relevantTypes[2].Id) ||
             outbox.Any(entry => entry.Context.EventType.Id == relevantTypes[2].Id)))
        {
            subscriptions.MarkAccepted(invitationId, organizationName);
        }
    }
}
