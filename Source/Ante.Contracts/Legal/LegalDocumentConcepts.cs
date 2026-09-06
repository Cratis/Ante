// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Ante.Contracts.Legal;

/// <summary>
/// The text of one legal document - typically the terms and conditions, or the privacy policy - as
/// markdown. A host supplies this through its own <c>ILegalDocumentSource</c> implementation; Ante
/// carries no legal text of its own.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record LegalDocumentBody(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="LegalDocumentBody"/>.
    /// </summary>
    public static readonly LegalDocumentBody NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="LegalDocumentBody"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator LegalDocumentBody(string value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="LegalDocumentBody"/> to its underlying <see cref="string"/> value.
    /// </summary>
    /// <param name="body">The value to convert from.</param>
    public static implicit operator string(LegalDocumentBody body) => body.Value;
}

/// <summary>
/// Identifies one revision of the legal document set a host presents through its
/// <c>ILegalDocumentSource</c>. Recorded on every acceptance so a client that has been open across a
/// document change, or one replaying an older payload, cannot record consent to a revision the user
/// never saw.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record LegalVersion(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="LegalVersion"/>.
    /// </summary>
    public static readonly LegalVersion NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="LegalVersion"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator LegalVersion(string value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="LegalVersion"/> to its underlying <see cref="string"/> value.
    /// </summary>
    /// <param name="version">The value to convert from.</param>
    public static implicit operator string(LegalVersion version) => version.Value;
}
