// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using System.Text.Json.Nodes;
using Ante.Contracts.Invitations;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;
using Cratis.Specifications;
using Cratis.Types;
using NSubstitute;
using Xunit;

namespace Ante.Contracts.for_EventSchemas.when_checking_contracts;

/// <summary>
/// Guards the Chronicle wire schemas of all published contract events, including historical generations.
/// </summary>
public class and_comparing_the_snapshot : Specification
{
    const string SnapshotName = "EventSchemas.snapshot.json";
    const string UpdateVariable = "ANTE_UPDATE_EVENT_SCHEMA_SNAPSHOT";
    static readonly JsonSerializerOptions _snapshotOptions = new() { WriteIndented = true };

    [Fact]
    void should_preserve_every_existing_generation()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Ante.Contracts.csproj")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        var path = Path.Combine(directory.FullName, SnapshotName);
        var pii = new PIIMetadataProvider();
        var generator = new JsonSchemaGenerator(
            new ComplianceMetadataResolver(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>(pii),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>(pii)),
            new DefaultNamingPolicy(),
            Substitute.For<IDerivedTypes>());

        var schemas = typeof(InvitationTokenIssued).Assembly.GetTypes()
            .Where(type => Attribute.IsDefined(type, typeof(EventTypeAttribute)) ||
                           type.GetCustomAttributes(false).Any(attribute =>
                               attribute.GetType().IsGenericType &&
                               attribute.GetType().GetGenericTypeDefinition() == typeof(EventTypeGenerationForAttribute<>)))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToDictionary(
                type => $"{type.GetEventType().Id.Value}:{type.GetEventType().Generation.Value}",
                type => SchemaFor(type, generator),
                StringComparer.Ordinal);

        Assert.NotEmpty(schemas);
        var existing = File.Exists(path)
            ? JsonSerializer.Deserialize<Dictionary<string, JsonNode>>(File.ReadAllText(path))!
            : new Dictionary<string, JsonNode>(StringComparer.Ordinal);
        var changed = existing.Keys.Intersect(schemas.Keys).Where(key =>
            !JsonNode.DeepEquals(existing[key], schemas[key])).ToArray();
        Assert.True(changed.Length == 0,
            $"Event schemas changed without a new generation: {string.Join(", ", changed)}. Bump the generation and add a migration; the snapshot cannot overwrite existing generations.");

        var missing = existing.Keys.Except(schemas.Keys).ToArray();
        Assert.True(missing.Length == 0,
            $"Previously snapshotted event generations are missing: {string.Join(", ", missing)}. Retain the previous generation record.");

        var added = schemas.Keys.Except(existing.Keys).ToArray();
        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            File.WriteAllText(path, JsonSerializer.Serialize(schemas, _snapshotOptions) + "\n");
            return;
        }

        Assert.True(added.Length == 0 && File.Exists(path),
            $"Event schema snapshot needs updating for: {string.Join(", ", added)}. Run {UpdateVariable}=1 dotnet test Source/Ante.Contracts/Ante.Contracts.csproj --filter FullyQualifiedName~and_comparing_the_snapshot, then commit the snapshot.");
    }

    static JsonNode SchemaFor(Type type, JsonSchemaGenerator generator)
    {
        var schema = JsonNode.Parse(generator.Generate(type).ToJson())!;
        var previousGeneration = type.GetCustomAttributes(false).FirstOrDefault(attribute =>
            attribute.GetType().IsGenericType &&
            attribute.GetType().GetGenericTypeDefinition() == typeof(EventTypeGenerationForAttribute<>));
        if (previousGeneration is not null)
        {
            // Chronicle ignores CLR titles when comparing stored schemas. Retain the original
            // event type's title when its prior generation is represented by a V1 record.
            schema["title"] = previousGeneration.GetType().GetGenericArguments()[0].Name;
        }

        return schema;
    }
}
#endif
