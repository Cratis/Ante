// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Ante.Contracts.Onboarding;

/// <summary>
/// Represents a generic, low-cardinality reason code a host gives with a terminal provisioning outcome, such as
/// quota-exceeded. It is shown to the person as a support reference, so it must never carry personal data,
/// tenant names or diagnostic detail. Ante accepts 1 to 64 characters of lowercase letters, digits, hyphen,
/// underscore and period, starting with a letter or digit, and refuses anything else.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record HostProvisioningReasonCode(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="HostProvisioningReasonCode"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator HostProvisioningReasonCode(string value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="HostProvisioningReasonCode"/> to its underlying <see cref="string"/> value.
    /// </summary>
    /// <param name="reasonCode">The value to convert from.</param>
    public static implicit operator string(HostProvisioningReasonCode reasonCode) => reasonCode.Value;
}
