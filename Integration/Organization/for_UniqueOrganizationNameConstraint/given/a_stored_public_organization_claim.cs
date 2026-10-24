// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Ante.Integration.given;
using Ante.Invitations.OrganizationSetup;
using Ante.Organization.Names;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Registrations;
using Cratis.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Ante.Integration.Organization.for_UniqueOrganizationNameConstraint.given;

public abstract class a_stored_public_organization_claim : Specification
{
    ChronicleClient _client;
    IMongoCollection<BsonDocument> _history;
    protected IEventStore Store;
    protected IAppendResult Result;
    protected IAppendResult UnrelatedClaim;
    protected abstract Type LegacyEventType { get; }
    protected abstract BsonDocument LegacyContent { get; }
    protected abstract string LegacyGeneration { get; }

    async Task Establish()
    {
        var infrastructure = ChronicleInfrastructure.Current;
        _client = new ChronicleClient(
            ChronicleOptions.FromConnectionString(infrastructure.ChronicleConnectionString),
            new legacy_claim_artifacts(),
            namingPolicy: new CamelCaseNamingPolicy());
        Store = await _client.GetEventStore($"LegacyClaims{Guid.NewGuid():N}");
        var registration = await Store.WaitForRegistration(TimeSpan.FromSeconds(30));
        registration.IsSuccess.ShouldBeTrue();
        _history = new MongoClient(infrastructure.MongoDBServer)
            .GetDatabase($"{Store.Name}+es+{Store.Namespace}")
            .GetCollection<BsonDocument>("event-log");

        await SeedStoredHistory();
        var constraints = new Constraints(Store, [new definitions(Store.EventTypes)]);
        await constraints.Discover();
        await constraints.Register();
        UnrelatedClaim = await Store.EventLog.Append(
            Guid.NewGuid().ToString(), new OrganizationNameReservationReceived("Another organization"));
    }

    async Task SeedStoredHistory()
    {
        // Obtain the kernel's storage envelope using a private event with no constraints registered yet.
        // Replace it at rest, not through Append/Outbox: public events can no longer be written to the log.
        var seed = await Store.EventLog.Append(
            Guid.NewGuid().ToString(), new OrganizationNameReservationReceived("Storage envelope"));
        seed.IsSuccess.ShouldBeTrue();
        var document = await _history.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        document["type"] = Store.EventTypes.GetEventTypeFor(LegacyEventType).Id.Value;
        document["content"] = new BsonDocument(LegacyGeneration, LegacyContent);
        document["contentHashes"] = new BsonDocument();
        document["appendedGeneration"] = int.Parse(LegacyGeneration, System.Globalization.CultureInfo.InvariantCulture);
        await _history.ReplaceOneAsync(new BsonDocument("_id", document["_id"]), document);
    }

    protected async Task AssertLegacyHistoryWasPreserved()
    {
        var type = Store.EventTypes.GetEventTypeFor(LegacyEventType);
        var stored = await _history.Find(new BsonDocument("type", type.Id.Value)).SingleAsync();
        stored["content"][LegacyGeneration]["tenantName"].AsString.ShouldEqual("Acme");
    }

    protected async Task AssertNothingWasPublished() =>
        (await Store.GetEventSequence(EventSequenceId.Outbox).GetTailSequenceNumber()).ShouldEqual(EventSequenceNumber.Unavailable);

    void Destroy() => _client.Dispose();

    sealed class definitions(IEventTypes eventTypes) : ICanProvideConstraints
    {
        public IImmutableList<IConstraintDefinition> Provide()
        {
            var builder = new ConstraintBuilder(eventTypes, new CamelCaseNamingPolicy());
            new UniqueOrganizationNameConstraint().Define(builder);
            return builder.Build();
        }
    }
}
