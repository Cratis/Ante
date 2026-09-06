// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Legal;

namespace Ante.Legal.for_LegalDocumentStatus.when_getting_current;

public class and_no_source_is_configured : Specification
{
    LegalDocumentStatus _result = null!;

    async Task Because() => _result = await LegalDocumentStatus.Current(new NoLegalDocumentSource());

    [Fact] void should_report_not_configured() => Assert.False(_result.IsConfigured);
}
#endif
