// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Legal;
using FluentValidation;

namespace Ante.Legal.for_LegalTermsRules.when_validating;

public record TestCommand(bool AcceptedLegalTerms, LegalVersion AcceptedLegalVersion);

public class TestValidator : AbstractValidator<TestCommand>
{
    public TestValidator(ILegalDocumentSource source) =>
        LegalTermsRules.Apply(this, source, c => c.AcceptedLegalTerms, c => c.AcceptedLegalVersion);
}

public class and_no_source_is_configured : Specification
{
    FluentValidation.Results.ValidationResult _result = null!;

    async Task Because() =>
        _result = await new TestValidator(new NoLegalDocumentSource())
            .ValidateAsync(new TestCommand(false, LegalVersion.NotSet));

    [Fact]
    void should_impose_no_requirement_to_accept_anything() => Assert.True(_result.IsValid);
}
#endif
