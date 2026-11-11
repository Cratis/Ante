// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Ante.Legal;

/// <summary>Generation 1 of <see cref="LegalTermsAcceptanceRecorded"/>, before the identity provider key and issuer were carried.</summary>
[EventTypeGenerationFor<LegalTermsAcceptanceRecorded>(1)]
public record LegalTermsAcceptanceRecordedV1(
    TenantName TenantName,
    IdentityProviderName IdentityProvider,
    string IdentityProviderSubject,
    LegalVersion Version);

/// <summary>
/// Migrates generation 1, which carried no identity provider key or issuer, leaving both empty. Ante never
/// guesses an issuer for a fact it recorded without one.
/// </summary>
public class LegalTermsAcceptanceRecordedMigration : EventTypeMigration<LegalTermsAcceptanceRecorded, LegalTermsAcceptanceRecordedV1>
{
    /// <inheritdoc/>
    public override void Upcast(IEventMigrationBuilder<LegalTermsAcceptanceRecorded, LegalTermsAcceptanceRecordedV1> builder) =>
        builder.Properties(properties => properties
            .DefaultValue(target => target.IdentityProviderKey, string.Empty)
            .DefaultValue(target => target.IdentityIssuer, string.Empty));

    /// <inheritdoc/>
    public override void Downcast(IEventMigrationBuilder<LegalTermsAcceptanceRecordedV1, LegalTermsAcceptanceRecorded> builder) =>
        builder.Properties(properties => { });
}
