// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Legal;
using Ante.Legal;

namespace Ante.Integration.given;

/// <summary>
/// A host-provided legal document set, so onboarding records <see cref="LegalTermsAccepted"/> evidence.
/// </summary>
public sealed class CurrentLegalDocuments : ILegalDocumentSource
{
    public static readonly LegalVersion Version = new("2026-01");

    public Task<LegalDocumentSet?> GetCurrent() =>
        Task.FromResult<LegalDocumentSet?>(new(new("Terms and conditions."), new("Privacy policy."), Version));
}
