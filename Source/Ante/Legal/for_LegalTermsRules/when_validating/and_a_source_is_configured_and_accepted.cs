// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Legal;

namespace Ante.Legal.for_LegalTermsRules.when_validating;

class configured_source(LegalDocumentSet documents) : ILegalDocumentSource
{
    public int Reads { get; private set; }

    public Task<LegalDocumentSet?> GetCurrent()
    {
        Reads++;
        return Task.FromResult<LegalDocumentSet?>(documents);
    }
}

public class and_a_source_is_configured_and_accepted : Specification
{
    static readonly LegalDocumentSet _current = new("# Terms", "# Privacy", "2026-01");

    readonly configured_source _source = new(_current);
    FluentValidation.Results.ValidationResult _result = null!;

    async Task Because() =>
        _result = await new TestValidator(_source)
            .ValidateAsync(new TestCommand(true, _current.Version));

    [Fact] void should_be_valid() => Assert.True(_result.IsValid);
    [Fact] void should_read_the_source_once() => _source.Reads.ShouldEqual(1);
}
#endif
