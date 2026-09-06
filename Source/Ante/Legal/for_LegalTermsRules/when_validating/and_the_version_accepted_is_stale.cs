// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Legal;

namespace Ante.Legal.for_LegalTermsRules.when_validating;

public class and_the_version_accepted_is_stale : Specification
{
    static readonly LegalDocumentSet _current = new("# Terms", "# Privacy", "2026-02");

    FluentValidation.Results.ValidationResult _result = null!;

    async Task Because() =>
        _result = await new TestValidator(new configured_source(_current))
            .ValidateAsync(new TestCommand(true, "2026-01"));

    [Fact]
    void should_not_be_valid() => Assert.False(_result.IsValid);

    [Fact]
    void should_report_the_version_field() =>
        Assert.Contains(_result.Errors, e => e.PropertyName == nameof(TestCommand.AcceptedLegalVersion));
}
#endif
