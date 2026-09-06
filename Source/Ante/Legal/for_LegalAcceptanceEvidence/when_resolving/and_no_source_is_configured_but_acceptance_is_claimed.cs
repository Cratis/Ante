// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Legal;

namespace Ante.Legal.for_LegalAcceptanceEvidence.when_resolving;

public class and_no_source_is_configured_but_acceptance_is_claimed : Specification
{
    Result<IEnumerable<object>, ValidationResult> _result = null!;

    async Task Because() =>
        _result = await LegalAcceptanceEvidence.Resolve(
            new NoLegalDocumentSource(),
            acceptedLegalTerms: true,
            acceptedLegalVersion: "2026-01",
            tenantName: "Acme",
            identityProvider: "github",
            subject: "subject-1");

    [Fact] void should_not_succeed() => Assert.False(_result.IsSuccess);

    [Fact]
    void should_report_unsolicited_acceptance()
    {
        _result.TryGetError(out var error);
        Assert.Equal(LegalTermsRules.UnsolicitedAcceptanceMessage, error.Message);
    }
}
#endif
