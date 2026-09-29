// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.LegacyConsumer;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Registrations;
using Cratis.Serialization;

// A real, isolated generation-2-only host reader. No generation-3 CLR type, migration or
// compliance metadata is loaded in this process. Never print the bearer token itself.
if (args.Length != 5)
{
    await Console.Error.WriteLineAsync("Expected operation, connection, host store, Ante store and invitation id.");
    return 2;
}

var (operation, connection, hostStore, anteStore, invitationId) = (args[0], args[1], args[2], args[3], args[4]);
using var client = new ChronicleClient(
    ChronicleOptions.FromConnectionString(connection),
    new LegacyArtifacts(),
    namingPolicy: new CamelCaseNamingPolicy());
var store = await client.GetEventStore(hostStore);
var registration = await store.WaitForRegistration(TimeSpan.FromSeconds(30));
if (!registration.IsSuccess)
{
    await Console.Error.WriteLineAsync($"Legacy host registration failed: {registration.Failure}");
    return 3;
}

if (operation == "setup")
{
    await store.Subscriptions.Subscribe(
        new EventStoreSubscriptionId(anteStore),
        anteStore,
        definition => definition.WithEventType(typeof(LegacyInvitationTokenIssued).GetEventType().Id));
    await Console.Out.WriteLineAsync("ready");
    return 0;
}

if (operation == "publish")
{
    var appended = await store.GetEventSequence(EventSequenceId.Outbox).Append(
        invitationId,
        new LegacyUserInvitedToJoinTenant($"{Guid.NewGuid():N}@example.com", "Acme", ["member"]),
        subject: new Subject(Guid.NewGuid().ToString()));
    await Console.Out.WriteLineAsync(appended.IsSuccess ? "published" : "failed");
    return appended.IsSuccess ? 0 : 6;
}

if (operation != "read")
{
    await Console.Error.WriteLineAsync("Unknown operation.");
    return 2;
}

var inbox = store.GetEventSequence(new EventSequenceId($"{EventSequenceId.InboxPrefix}{anteStore}"));
var events = await inbox.GetForEventSourceIdAndEventTypes(invitationId, [typeof(LegacyInvitationTokenIssued).GetEventType()]);
var token = events.Select(entry => entry.Content).OfType<LegacyInvitationTokenIssued>().FirstOrDefault()?.Token;
if (token is null)
{
    await Console.Out.WriteLineAsync("missing");
    return 4;
}

var segments = token.Split('.');
var usable = segments.Length == 3 && segments.All(part => part.Length > 0) && segments[0].StartsWith("eyJ", StringComparison.Ordinal);
await Console.Out.WriteLineAsync(usable ? "readable" : "unreadable");
return usable ? 0 : 5;
