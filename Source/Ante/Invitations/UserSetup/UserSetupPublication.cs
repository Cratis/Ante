// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Legal;
using Ante.Outbox;
using MongoDB.Driver;

namespace Ante.Invitations.UserSetup;

/// <summary>
/// Durable record of what a join-tenant invitation's acceptance locally committed to Ante's own event
/// log - the "Recorded" boundary from the Recorded/Published distinction the onboarding adoption epic
/// proposes. <see cref="AcceptanceRecorded"/> is set the moment <see cref="AcceptInvitation"/>'s Handle()
/// appends successfully; <see cref="LegalRecorded"/> distinguishes whether a
/// <see cref="LegalTermsAccepted"/> fact was part of that same append, since nothing else durably
/// remembers that once the append itself has happened.
/// </summary>
/// <param name="Id">The invitation identifier.</param>
/// <param name="AcceptanceRecorded">Whether the acceptance itself was recorded.</param>
/// <param name="LegalRecorded">Whether a <see cref="LegalTermsAccepted"/> fact was recorded alongside the acceptance.</param>
[ReadModel]
[FromEvent<InvitationToJoinTenantAccepted>]
[FromEvent<LegalTermsAccepted>]
public record UserSetupProgress(
    InvitationId Id,
    [SetValue<InvitationToJoinTenantAccepted>(true)] bool AcceptanceRecorded = false,
    [SetValue<LegalTermsAccepted>(true)] bool LegalRecorded = false);

/// <summary>
/// Durable evidence that a join-tenant invitation's public facts have reached Ante's own outbox - the
/// "Published" boundary. <see cref="AcceptancePublished"/> is set once the acceptance itself has reached
/// the outbox; <see cref="LegalPublished"/> mirrors the legal fact separately reaching the outbox, when
/// one was recorded locally.
/// </summary>
/// <param name="Id">The invitation identifier.</param>
/// <param name="AcceptancePublished">Whether the acceptance itself has reached the outbox.</param>
/// <param name="LegalPublished">Whether a <see cref="LegalTermsAccepted"/> fact has reached the outbox for this invitation.</param>
[ReadModel]
[EventSequence(EventSequenceId.OutboxId)]
[FromEvent<InvitationToJoinTenantAccepted>]
[FromEvent<LegalTermsAccepted>]
public record JoinTenantAcceptancePublished(
    InvitationId Id,
    [SetValue<InvitationToJoinTenantAccepted>(true)] bool AcceptancePublished = false,
    [SetValue<LegalTermsAccepted>(true)] bool LegalPublished = false);

/// <summary>
/// The pure decision of whether a join-tenant invitation's acceptance - including its legal fact, when
/// one was recorded - has fully reached Ante's outbox. Kept free of the Mongo round-trips so it can be
/// exercised directly from a spec.
/// </summary>
public static class JoinTenantPublication
{
    /// <summary>
    /// Determines whether every fact this invitation's acceptance recorded locally has also reached the
    /// outbox.
    /// </summary>
    /// <param name="recorded">The durable local record, or null when acceptance was never recorded.</param>
    /// <param name="published">The durable outbox record, or null when nothing has been published yet.</param>
    /// <returns>True when fully published; otherwise false.</returns>
    public static bool IsFullyPublished(UserSetupProgress? recorded, JoinTenantAcceptancePublished? published) =>
        recorded is { AcceptanceRecorded: true } &&
        published is { AcceptancePublished: true } &&
        (!recorded.LegalRecorded || published.LegalPublished);
}

/// <summary>
/// Accelerates the join-tenant flow's live status subscription once its durable evidence - across both
/// the local record and the outbox - confirms full publication.
/// </summary>
/// <param name="recordedCollection">The durable acceptance-record collection.</param>
/// <param name="publishedCollection">The durable outbox-publication collection.</param>
/// <param name="subscriptions">The subscription tracker.</param>
public class JoinTenantPublicationStatusNotifier(
    IMongoCollection<UserSetupProgress> recordedCollection,
    IMongoCollection<JoinTenantAcceptancePublished> publishedCollection,
    UserSetupStatusSubscriptions subscriptions) : IPublicationStatusNotifier
{
    /// <inheritdoc/>
    public async Task NotifyIfPublished(EventSourceId eventSourceId)
    {
        var invitationId = (InvitationId)Guid.Parse(eventSourceId.Value);
        var recorded = await recordedCollection.Find(Builders<UserSetupProgress>.Filter.Eq(progress => progress.Id, invitationId)).FirstOrDefaultAsync();
        if (recorded is null)
        {
            // Not a join-tenant invitation - most likely the shared LegalTermsAcceptanceOutbox checking
            // on behalf of a different flow's acceptance.
            return;
        }

        var published = await publishedCollection.Find(Builders<JoinTenantAcceptancePublished>.Filter.Eq(progress => progress.Id, invitationId)).FirstOrDefaultAsync();
        if (JoinTenantPublication.IsFullyPublished(recorded, published))
        {
            subscriptions.MarkAccepted(invitationId);
        }
    }
}
