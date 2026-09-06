// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Legal;

namespace Ante.Legal.for_LegalAcceptanceEvidence.when_resolving;

class configured_source(LegalDocumentSet documents) : ILegalDocumentSource
{
    public Task<LegalDocumentSet?> GetCurrent() => Task.FromResult<LegalDocumentSet?>(documents);
}

public class and_a_source_is_configured_and_accepted_with_the_current_version : Specification
{
    static readonly LegalDocumentSet _current = new("# Terms", "# Privacy", "2026-01");

    Result<IEnumerable<object>, ValidationResult> _result = null!;

    async Task Because() =>
        _result = await LegalAcceptanceEvidence.Resolve(
            new configured_source(_current),
            acceptedLegalTerms: true,
            acceptedLegalVersion: _current.Version,
            tenantName: "Acme",
            identityProvider: "github",
            subject: "subject-1");

    [Fact] void should_succeed() => Assert.True(_result.IsSuccess);

    [Fact]
    void should_append_exactly_one_legal_terms_accepted_event()
    {
        _result.TryGetResult(out var events);
        Assert.Single(events);
    }

    [Fact]
    void should_carry_the_current_version()
    {
        _result.TryGetResult(out var events);
        var accepted = (LegalTermsAccepted)events.Single();
        Assert.Equal(_current.Version, accepted.Version);
    }

    [Fact]
    void should_carry_the_tenant_identity_provider_and_subject()
    {
        _result.TryGetResult(out var events);
        var accepted = (LegalTermsAccepted)events.Single();
        Assert.Equal("Acme", (string)accepted.TenantName);
        Assert.Equal("github", (string)accepted.IdentityProvider);
        Assert.Equal("subject-1", accepted.IdentityProviderSubject);
    }
}
#endif
