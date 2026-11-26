// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Invitations.Accepting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Ante.Invitations.Receiving;

/// <summary>
/// Removes the Mongo documents Ante keeps for one invitation outside Chronicle's encrypted events.
/// </summary>
public interface IInvitationDocumentEraser
{
    /// <summary>
    /// Deletes every staged transaction and exchange session of the invitation.
    /// </summary>
    /// <param name="invitationId">The invitation whose documents are removed.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Erase(InvitationId invitationId);
}

/// <summary>
/// Deletes the legacy exchange sessions, the staged attested transactions and the attested sessions of an invitation.
/// </summary>
/// <param name="scopeFactory">Creates the scope the collections are resolved from.</param>
public class MongoInvitationDocumentEraser(IServiceScopeFactory scopeFactory) : IInvitationDocumentEraser
{
    /// <inheritdoc/>
    public async Task Erase(InvitationId invitationId)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var key = new BsonBinaryData(invitationId.Value, GuidRepresentation.Standard);
        await DeleteFor<AcceptedInvitation>(services, key);
        await DeleteFor<StagedInvitationTransaction>(services, key);
        await DeleteFor<AttestedInvitationSession>(services, key);
    }

    static Task<DeleteResult> DeleteFor<TDocument>(IServiceProvider services, BsonBinaryData key) =>
        services.GetRequiredService<IMongoCollection<TDocument>>()
            .DeleteManyAsync(Builders<TDocument>.Filter.Eq(InviteExchangeProcessor.InvitationIdField, key));
}

/// <summary>
/// Erases Ante's copy of an invitation when the host asks, and acknowledges it only when nothing is left.
/// </summary>
/// <param name="eventStore">Ante's event store, which holds the log, the outbox and the encryption keys.</param>
/// <param name="documents">Removes the Mongo copies outside the event log.</param>
/// <param name="logger">Logs the erasure.</param>
public class InvitationErasureReceiver(IEventStore eventStore, IInvitationDocumentEraser documents, ILogger logger)
{
    /// <summary>
    /// Makes the invitation unacceptable, removes the plaintext copies and deletes the compliance subject's key.
    /// </summary>
    /// <param name="invitationId">The invitation to erase.</param>
    /// <param name="context">The inbox delivery of the request.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the invitation cannot be fenced against acceptance.</exception>
    public async Task Erase(InvitationId invitationId, EventContext context)
    {
        // A revocation concludes the invitation for every fence - stage, completion, acceptance, token issuance and
        // a later delivery of the invitation itself - so nothing can recreate what is about to be removed.
        var revocation = typeof(InvitationRevocationReceived).GetEventType();
        var history = await eventStore.EventLog.GetForEventSourceIdAndEventTypes(context.EventSourceId, [revocation]);
        if (history.Count == 0)
        {
            var result = await eventStore.EventLog.Append(
                context.EventSourceId,
                new InvitationRevocationReceived(),
                correlationId: context.CorrelationId,
                occurred: context.Occurred,
                subject: context.Subject);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to fence invitation {context.EventSourceId} against acceptance before erasure.");
            }
        }

        await documents.Erase(invitationId);
        await eventStore.PII.DeleteEncryptionKeyFor(context.Subject);
        logger.LogInvitationErased(context.EventSourceId.Value);
    }
}

internal static partial class InvitationErasureLogging
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Erased the invitation {InvitationId} from Ante.")]
    internal static partial void LogInvitationErased(this ILogger logger, string invitationId);
}
