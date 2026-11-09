// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Concepts;

namespace Ante.Contracts.Organization;

/// <summary>
/// Represents the message a reviewer wrote when deciding on a sign-up that was held for review. It is addressed to
/// the person who signed up, so it may contain personal data.
/// </summary>
/// <param name="Value">The underlying value.</param>
[PII]
public record ReviewerMessage(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="ReviewerMessage"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator ReviewerMessage(string value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="ReviewerMessage"/> to its underlying <see cref="string"/> value.
    /// </summary>
    /// <param name="message">The value to convert from.</param>
    public static implicit operator string(ReviewerMessage message) => message.Value;
}
