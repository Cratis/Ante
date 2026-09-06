// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Legal;

namespace Ante.Legal.for_LegalAcceptanceEvidence.when_resolving;

public class and_no_source_is_configured_and_acceptance_is_not_claimed : Specification
{
    Result<IEnumerable<object>, ValidationResult> _result = null!;

    async Task Because() =>
        _result = await LegalAcceptanceEvidence.Resolve(
            new NoLegalDocumentSource(),
            acceptedLegalTerms: false,
            acceptedLegalVersion: LegalVersion.NotSet,
            tenantName: "Acme",
            identityProvider: "github",
            subject: "subject-1");

    [Fact] void should_succeed() => Assert.True(_result.IsSuccess);

    [Fact]
    void should_have_no_events_to_append()
    {
        _result.TryGetResult(out var events);
        Assert.Empty(events);
    }
}
#endif
