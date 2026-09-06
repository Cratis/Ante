// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Legal;

namespace Ante.Legal.for_LegalDocumentStatus.when_getting_current;

class configured_source(LegalDocumentSet documents) : ILegalDocumentSource
{
    public Task<LegalDocumentSet?> GetCurrent() => Task.FromResult<LegalDocumentSet?>(documents);
}

public class and_a_source_is_configured : Specification
{
    static readonly LegalDocumentSet _current = new("# Terms", "# Privacy", "2026-01");

    LegalDocumentStatus _result = null!;

    async Task Because() => _result = await LegalDocumentStatus.Current(new configured_source(_current));

    [Fact] void should_report_configured() => Assert.True(_result.IsConfigured);
    [Fact] void should_carry_the_terms() => Assert.Equal(_current.TermsAndConditions, _result.TermsAndConditions);
    [Fact] void should_carry_the_version() => Assert.Equal(_current.Version, _result.Version);
}
#endif
