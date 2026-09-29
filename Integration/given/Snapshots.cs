// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Ante.Integration.given;

/// <summary>
/// Stable text forms of durable state, so state taken before and after a replay or a lost checkpoint compares line by
/// line and a failure shows exactly what was added, lost or altered.
/// </summary>
public static class Snapshots
{
    /// <summary>
    /// Renders everything that identifies an appended event and everything a consumer can read from it: sequence number,
    /// type and generation, event source, stream, occurrence, correlation, compliance subject, content hash, causation
    /// chain and every property of the content.
    /// </summary>
    /// <param name="appended">The appended event.</param>
    /// <returns>One line describing the event.</returns>
    public static string Identity(AppendedEvent appended)
    {
        var context = appended.Context;
        var causation = string.Join(
            "; ",
            context.Causation.Select(cause =>
                $"{cause.Type} at {cause.Occurred.UtcDateTime:O} {{{string.Join(", ", cause.Properties.OrderBy(property => property.Key, StringComparer.Ordinal).Select(property => $"{property.Key}={property.Value}"))}}}"));
        return $"#{context.SequenceNumber} {context.EventType} source={context.EventSourceType}/{context.EventSourceId} " +
            $"stream={context.EventStreamType}/{context.EventStreamId} occurred={context.Occurred.UtcDateTime:O} " +
            $"correlation={context.CorrelationId} subject={context.Subject} hash={context.Hash} causation=[{causation}] " +
            $"content={OnboardingTrace.Flatten(appended.Content)}";
    }

    /// <summary>
    /// Renders every event in a sequence, in sequence order.
    /// </summary>
    /// <param name="sequence">The event sequence.</param>
    /// <returns>One line per event.</returns>
    public static async Task<IReadOnlyList<string>> Of(IEventSequence sequence) =>
        [.. (await sequence.GetFromSequenceNumber(EventSequenceNumber.First)).Select(Identity)];

    /// <summary>
    /// Renders every document a read model's projection has stored, ordered by key: the stored document itself and the
    /// instance the kernel serves for it, with personal data released.
    /// </summary>
    /// <remarks>
    /// The kernel encrypts a <c>[PII]</c> value with a fresh nonce every time it writes it, so a rebuilt document holds
    /// different ciphertext for the same value. The stored form therefore shows only that a value is encrypted, and the
    /// released instance - what <c>IReadModels.GetInstanceById</c> returns, decrypted by the kernel - shows the value.
    /// </remarks>
    /// <typeparam name="TReadModel">The read model.</typeparam>
    /// <param name="ante">The running Ante, whose own collection mapping names the collection.</param>
    /// <returns>One line per document.</returns>
    public static async Task<IReadOnlyList<string>> Of<TReadModel>(AnteApplication ante)
    {
        await using var scope = ante.Services.CreateAsyncScope();
        var typed = scope.ServiceProvider.GetRequiredService<IMongoCollection<TReadModel>>();
        var readModels = scope.ServiceProvider.GetRequiredService<IEventStore>().ReadModels;
        var stored = typed.Database.GetCollection<BsonDocument>(typed.CollectionNamespace.CollectionName);
        var lines = new List<string>();
        foreach (var document in await stored.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync())
        {
            var id = document["_id"];
            var key = id is BsonBinaryData { SubType: BsonBinarySubType.UuidStandard } uuid
                ? uuid.ToGuid(GuidRepresentation.Standard).ToString("D")
                : id.AsString;
            var released = await readModels.GetInstanceById<TReadModel>(key);
            lines.Add($"{Masked(document).ToJson()} released={OnboardingTrace.Flatten(released)}");
        }

        return [.. lines.Order(StringComparer.Ordinal)];
    }

    /// <summary>
    /// Joins a snapshot into one text, so a comparison failure prints both sides in full.
    /// </summary>
    /// <param name="lines">The snapshot.</param>
    /// <returns>The lines, one per row.</returns>
    public static string Text(IEnumerable<string> lines) => string.Join(Environment.NewLine, lines);

    // The kernel's encryption envelope starts with the bytes "CENV", which is "Q0VOV" in base64.
    static BsonValue Masked(BsonValue value) => value switch
    {
        BsonString text when text.Value.StartsWith("Q0VOV", StringComparison.Ordinal) => new BsonString("<encrypted>"),
        BsonDocument document => new BsonDocument(document.Select(element => new BsonElement(element.Name, Masked(element.Value)))),
        BsonArray items => new BsonArray(items.Select(Masked)),
        _ => value,
    };
}
