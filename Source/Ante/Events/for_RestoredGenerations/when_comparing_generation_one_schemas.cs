// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;
using Ante.Invitations.OrganizationSetup;
using Ante.Invitations.UserSetup;
using Ante.Legal;
using Ante.Organization.Registration;
using Ante.Organization.Registration.Review;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;
using Cratis.Types;

namespace Ante.Events.for_RestoredGenerations;

/// <summary>
/// Chronicle refuses an event type whose generation-1 schema differs from the one it stored. Ante 1.9.0 stored these
/// five events, so each generation-1 record must still describe exactly the schema 1.9.0 produced.
/// </summary>
public class when_comparing_generation_one_schemas : Specification
{
    static readonly Type[] _generationOneTypes =
    [
        typeof(OrganizationSetupCompletedV1),
        typeof(UserSetupCompletedV1),
        typeof(LegalTermsAcceptanceRecordedV1),
        typeof(OrganizationRegistrationRecordedV1),
        typeof(OrganizationRegistrationHeldV1),
    ];

    JsonNode _released = null!;
    JsonSchemaGenerator _generator = null!;

    void Establish()
    {
        var pii = new PIIMetadataProvider();
        _generator = new JsonSchemaGenerator(
            new ComplianceMetadataResolver(new Instances<ICanProvideComplianceMetadataForType>(pii), new Instances<ICanProvideComplianceMetadataForProperty>(pii)),
            new CamelCaseNamingPolicy());
        _released = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Events", "for_RestoredGenerations", "Release1.9.0Schemas.json")))!;
    }

    [Fact]
    void should_describe_every_generation_one_record_as_released_in_1_9_0() =>
        Assert.All(_generationOneTypes, type =>
        {
            var name = type.Name[..^2];
            var schema = JsonNode.Parse(_generator.Generate(type).ToJson())!;

            // Chronicle ignores CLR titles when comparing stored schemas; keep the original event's title.
            schema["title"] = name;
            Assert.True(JsonNode.DeepEquals(_released[name], schema), $"{name} generation 1 differs from the 1.9.0 schema.");
        });

    sealed class Instances<T>(params T[] instances) : IInstancesOf<T>
        where T : class
    {
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)instances).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
#endif
