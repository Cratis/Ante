// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts;

namespace Ante.Outbox;

/// <summary>
/// Carries the requester's identity from an inbound request to the replies Ante publishes for it.
/// </summary>
/// <remarks>
/// A host addresses a request to this lobby (<c language="csharp">audience=&lt;lobby&gt;</c>) and names itself with
/// <see cref="AudienceTags.From"/>. <see cref="Inbound"/> re-stamps <c language="csharp">from</c> on every event Ante appends while it
/// handles that request, so the receipt in Ante's own log remembers who asked. <see cref="Resolve"/> later finds that
/// requester - on the trigger itself, else on the earliest tagged event of the same event source, else the configured
/// <c language="csharp">Ante:OperatorAudience</c> - and opens a Chronicle audience scope so the outbox append is addressed to it.
/// With no requester and no fallback the reply stays unaddressed, exactly as before audiences existed.
/// </remarks>
public static class ReplyAudience
{
    static readonly IDisposable _none = new NoScope();

    /// <summary>
    /// Re-stamps the requester carried by an inbound event on everything appended while the returned scope is open.
    /// </summary>
    /// <param name="inbound">The context of the inbound request.</param>
    /// <returns>The scope to dispose when the request has been handled.</returns>
    public static IDisposable Inbound(EventContext inbound) =>
        RequesterOf(inbound) is { } requester ? EventTags.Scope(AudienceTags.From, requester) : _none;

    /// <summary>
    /// Finds the audience a reply to the trigger is addressed to.
    /// </summary>
    /// <param name="eventStore">The event store holding the request's history.</param>
    /// <param name="trigger">The event whose publication is the reply.</param>
    /// <param name="operatorAudience">The configured fallback audience; empty or <see langword="null"/> means none.</param>
    /// <returns>The audience, or <see langword="null"/> when the reply is not addressed.</returns>
    public static async Task<string?> Resolve(IEventStore eventStore, EventContext trigger, string? operatorAudience)
    {
        var audience = RequesterOf(trigger);
        if (audience is null)
        {
            var history = await eventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, trigger.EventSourceId);
            audience = history.Select(entry => RequesterOf(entry.Context)).FirstOrDefault(requester => requester is not null);
        }

        return audience ?? (string.IsNullOrEmpty(operatorAudience) ? null : operatorAudience);
    }

    /// <summary>
    /// Opens the scope that addresses outbox appends to an audience. Call it from the method that appends, not from an
    /// <see langword="async"/> helper: an ambient scope does not flow back out of an awaited method.
    /// </summary>
    /// <param name="audience">The resolved audience, if any.</param>
    /// <returns>The scope to dispose when the reply has been appended.</returns>
    public static IDisposable Address(string? audience) => audience is null ? _none : EventTags.Audience(audience);

    static string? RequesterOf(EventContext context) =>
        context.NamedTags.FirstOrDefault(tag => string.Equals(tag.Name.Value, AudienceTags.From, StringComparison.Ordinal))?.Value;

    sealed class NoScope : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
