// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Legal;

namespace Ante.Legal.for_LegalAcceptanceEvidence.when_resolving;

public class and_the_version_accepted_is_stale : Specification
{
    static readonly LegalDocumentSet _current = new("# Terms", "# Privacy", "2026-02");

    Result<IEnumerable<object>, ValidationResult> _result = null!;

    async Task Because() =>
        _result = await LegalAcceptanceEvidence.Resolve(
            new configured_source(_current),
            acceptedLegalTerms: true,
            acceptedLegalVersion: "2026-01",
            tenantName: "Acme",
            identityProvider: "github",
            subject: "subject-1");

    [Fact] void should_not_succeed() => Assert.False(_result.IsSuccess);

    [Fact]
    void should_report_that_the_terms_have_changed()
    {
        _result.TryGetError(out var error);
        Assert.Equal(LegalTermsRules.StaleVersionMessage, error.Message);
    }
}
#endif
