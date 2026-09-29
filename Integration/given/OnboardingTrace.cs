// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Integration.given;

/// <summary>
/// Everything one onboarding left at each hop between a host and Ante, read back through the Chronicle client:
/// host outbox, Ante's inbox for that host, Ante's event log, Ante's outbox and the host's receipt inbox.
/// </summary>
/// <remarks>
/// Every hop is read as a whole sequence, not filtered by event source id: each spec class has its own host store
/// and Ante store, so nothing but that one onboarding is in them, and an event that landed under another id, or a
/// duplicate, is part of what the spec compares instead of being filtered away.
/// </remarks>
/// <param name="EventSourceId">The invitation or registration id the onboarding runs under.</param>
/// <param name="HostOutbox">What the host published (empty for self-service registration, which has no host input).</param>
/// <param name="AnteInbox">What Chronicle forwarded into Ante's inbox for that host.</param>
/// <param name="AnteLog">Everything in Ante's own event log.</param>
/// <param name="AnteOutbox">What Ante published to its outbox.</param>
/// <param name="HostReceipt">What reached the host in <c>inbox-{ante store}</c>.</param>
public sealed record OnboardingTrace(
    string EventSourceId,
    IReadOnlyList<AppendedEvent> HostOutbox,
    IReadOnlyList<AppendedEvent> AnteInbox,
    IReadOnlyList<AppendedEvent> AnteLog,
    IReadOnlyList<AppendedEvent> AnteOutbox,
    IReadOnlyList<AppendedEvent> HostReceipt)
{
    /// <summary>
    /// Reads every hop for one event source.
    /// </summary>
    /// <param name="ante">The running Ante.</param>
    /// <param name="host">The host store the onboarding belongs to.</param>
    /// <param name="eventSourceId">The invitation or registration id (canonical <c>D</c> format).</param>
    /// <returns>The trace of that moment.</returns>
    public static async Task<OnboardingTrace> Capture(AnteApplication ante, HostStore host, string eventSourceId)
    {
        await using var scope = ante.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
        return new(
            eventSourceId,
            await All(host.Outbox),
            await All(store.GetEventSequence(new EventSequenceId($"{EventSequenceId.InboxPrefix}{host.Name}"))),
            await All(store.EventLog),
            await All(store.GetEventSequence(EventSequenceId.Outbox)),
            await All(host.InboxFromAnte));
    }

    /// <summary>
    /// Waits until every hop satisfies <paramref name="complete"/>, then waits <paramref name="settle"/> and reads
    /// again so a duplicate that arrives late is part of what the spec compares.
    /// </summary>
    /// <param name="ante">The running Ante.</param>
    /// <param name="host">The host store the onboarding belongs to.</param>
    /// <param name="eventSourceId">The invitation or registration id.</param>
    /// <param name="complete">Whether the trace already holds every expected fact.</param>
    /// <param name="settle">How long to keep watching for late duplicates once complete.</param>
    /// <returns>The settled trace.</returns>
    public static async Task<OnboardingTrace> CaptureSettled(
        AnteApplication ante,
        HostStore host,
        string eventSourceId,
        Func<OnboardingTrace, bool> complete,
        TimeSpan? settle = default)
    {
        OnboardingTrace? last = default;
        await Eventually.Until(
            async () =>
            {
                last = await Capture(ante, host, eventSourceId);
                return complete(last);
            },
            what: $"every hop of {eventSourceId} to hold its expected facts");
        await Task.Delay(settle ?? TimeSpan.FromSeconds(3));
        return await Capture(ante, host, eventSourceId);
    }

    /// <summary>
    /// Renders every public property of a payload, recursively, so two payloads compare on every property and a
    /// property added to a contract later is compared without the spec being touched.
    /// </summary>
    /// <param name="value">The payload.</param>
    /// <returns>A stable text form of the payload.</returns>
    public static string Flatten(object? value) => value switch
    {
        null => "null",
        string text => $"\"{text}\"",
        DateTimeOffset moment => moment.UtcDateTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
        System.Collections.IEnumerable items => $"[{string.Join(", ", items.Cast<object?>().Select(Flatten))}]",
        var other when other.GetType().IsPrimitive || other is Enum || other is Guid => other.ToString()!,
        var other => $"{other.GetType().Name}{{{string.Join(", ", other.GetType().GetProperties().Where(property => property.GetIndexParameters().Length == 0).Select(property => $"{property.Name}={Flatten(property.GetValue(other))}"))}}}",
    };

    /// <summary>
    /// The events of one type at a hop, in sequence order.
    /// </summary>
    /// <typeparam name="TEvent">The event's CLR type.</typeparam>
    /// <param name="hop">The hop's events.</param>
    /// <returns>The matching appended events.</returns>
    public static IReadOnlyList<AppendedEvent> Of<TEvent>(IReadOnlyList<AppendedEvent> hop) =>
        [.. hop.Where(appended => appended.Content is TEvent)];

    /// <summary>
    /// The single event of one type at a hop, or a failure naming the count when there is not exactly one.
    /// </summary>
    /// <typeparam name="TEvent">The event's CLR type.</typeparam>
    /// <param name="hop">The hop's events.</param>
    /// <returns>The appended event, with its context.</returns>
    public static AppendedEvent TheOne<TEvent>(IReadOnlyList<AppendedEvent> hop)
    {
        var matching = Of<TEvent>(hop);
        return matching.Count == 1
            ? matching[0]
            : throw new InvalidOperationException($"Expected exactly one {typeof(TEvent).Name} but found {matching.Count} in [{Describe(hop)}].");
    }

    /// <summary>
    /// The event type names at a hop, sorted, so a hop compares as a multiset: a duplicate or a missing fact changes
    /// the list, while the order independent reactors happen to publish in does not.
    /// </summary>
    /// <param name="hop">The hop's events.</param>
    /// <returns>The sorted event type names.</returns>
    public static string Types(IReadOnlyList<AppendedEvent> hop) =>
        string.Join(", ", hop.Select(appended => appended.Content?.GetType().Name ?? appended.Context.EventType.ToString()).Order(StringComparer.Ordinal));

    /// <summary>
    /// Reads the claims of an invitation token without validating it.
    /// </summary>
    /// <param name="token">The signed JWT.</param>
    /// <returns>The payload's claims as JSON.</returns>
    public static System.Text.Json.JsonElement TokenClaims(string token)
    {
        var payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        return System.Text.Json.JsonDocument.Parse(Convert.FromBase64String(payload)).RootElement.Clone();
    }

    /// <summary>
    /// Renders a hop for a failure message.
    /// </summary>
    /// <param name="hop">The hop's events.</param>
    /// <returns>One line per event: type, sequence, correlation, subject.</returns>
    public static string Describe(IReadOnlyList<AppendedEvent> hop)
    {
        var text = new StringBuilder();
        foreach (var appended in hop)
        {
            var context = appended.Context;
            text.Append($"{appended.Content?.GetType().Name ?? context.EventType.ToString()}#{context.SequenceNumber} corr={context.CorrelationId} subj={context.Subject} | ");
        }

        return text.ToString();
    }

    static async Task<IReadOnlyList<AppendedEvent>> All(IEventSequence sequence) =>
        [.. await sequence.GetFromSequenceNumber(EventSequenceNumber.First)];
}
