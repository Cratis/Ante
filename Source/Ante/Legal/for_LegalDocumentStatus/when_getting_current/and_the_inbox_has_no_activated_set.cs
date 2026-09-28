// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;

namespace Ante.Legal.for_LegalDocumentStatus.when_getting_current;

public class and_the_inbox_has_no_activated_set : Specification
{
    readonly ILegalDocumentSource _source = new empty_required_source();
    LegalDocumentStatus _status = null!;
    Result<IEnumerable<object>, Cratis.Arc.Validation.ValidationResult> _acceptance;

    async Task Because()
    {
        _status = await LegalDocumentStatus.Current(_source);
        _acceptance = await LegalAcceptanceEvidence.Resolve(
            _source,
            false,
            LegalVersion.NotSet,
            "Acme",
            "github",
            "user-1");
    }

    [Fact] void should_report_unavailable_instead_of_an_optional_step() => _status.IsUnavailable.ShouldBeTrue();
    [Fact] void should_not_report_configured_documents() => _status.IsConfigured.ShouldBeFalse();
    [Fact] void should_reject_onboarding_before_activation() => _acceptance.TryGetError(out _).ShouldBeTrue();

    class empty_required_source : ILegalDocumentSource, ILegalDocumentAvailability
    {
        public bool RequiresDocuments => true;

        public Task<LegalDocumentSet?> GetCurrent() => Task.FromResult<LegalDocumentSet?>(null);
    }
}
#endif
