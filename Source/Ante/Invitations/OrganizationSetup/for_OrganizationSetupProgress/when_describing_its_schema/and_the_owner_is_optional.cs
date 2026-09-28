// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;
using Cratis.Types;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupProgress.when_describing_its_schema;

/// <summary>
/// ReadModelScenario substitutes the sink, so it cannot see what the kernel does with the registered schema. The
/// kernel only returns properties whose schema has a type, and only encrypts properties whose schema carries
/// compliance metadata. A nullable concept constructor parameter defaulted to null is described without either,
/// which kept the registration owner out of every instance read back - so every registrant saw Pending forever.
/// </summary>
public class and_the_owner_is_optional : Specification
{
    JsonSchema _schema = null!;

    void Because()
    {
        var pii = new PIIMetadataProvider();
        var generator = new JsonSchemaGenerator(
            new ComplianceMetadataResolver(new Instances<ICanProvideComplianceMetadataForType>(pii), new Instances<ICanProvideComplianceMetadataForProperty>(pii)),
            new CamelCaseNamingPolicy());
        _schema = generator.Generate(typeof(OrganizationSetupProgress));
    }

    [Fact] void should_describe_the_owner_subject_as_a_string() => Assert.True(_schema.ActualProperties["ownerSubject"].Type.HasFlag(JsonObjectType.String));
    [Fact] void should_classify_the_owner_subject_as_personal_data() => Assert.NotEmpty(_schema.ActualProperties["ownerSubject"].GetComplianceMetadata());
    [Fact] void should_describe_the_owner_provider_as_a_string() => Assert.True(_schema.ActualProperties["ownerProvider"].Type.HasFlag(JsonObjectType.String));
    [Fact] void should_not_require_an_owner_for_invited_setup() => Assert.DoesNotContain(
        "ownerSubject",
        JsonNode.Parse(_schema.ToJson())!["required"]!.AsArray().Select(name => name!.GetValue<string>()));

    sealed class Instances<T>(params T[] instances) : IInstancesOf<T>
        where T : class
    {
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)instances).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
#endif
