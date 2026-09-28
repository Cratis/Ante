// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Ante.Contracts.Legal;

/// <summary>
/// The host's monotonic revision of its complete legal document set.
/// </summary>
/// <param name="Value">The positive revision number.</param>
public record LegalDocumentRevision(long Value) : ConceptAs<long>(Value)
{
    /// <summary>
    /// Converts a revision number to its domain value.
    /// </summary>
    /// <param name="value">The revision number.</param>
    public static implicit operator LegalDocumentRevision(long value) => new(value);
}

/// <summary>
/// A complete, immutable pair of documents published to the host outbox on the configured legal set stream.
/// A version identifies the exact pair, even if a later revision is delivered before an earlier one.
/// </summary>
/// <param name="Revision">A positive number that increases for each new set.</param>
/// <param name="Version">The host-owned identity for the complete pair.</param>
/// <param name="TermsAndConditions">The full terms body.</param>
/// <param name="PrivacyPolicy">The full privacy body.</param>
[EventType]
public record LegalDocumentSetPublished(
    LegalDocumentRevision Revision,
    LegalVersion Version,
    LegalDocumentBody TermsAndConditions,
    LegalDocumentBody PrivacyPolicy);

/// <summary>
/// Acknowledges that Ante has durably activated a host-published set for onboarding.
/// Published to Ante's outbox on the same configured set stream.
/// </summary>
/// <param name="Revision">The activated revision.</param>
/// <param name="Version">The activated version.</param>
[EventType]
public record LegalDocumentSetActivated(LegalDocumentRevision Revision, LegalVersion Version);
