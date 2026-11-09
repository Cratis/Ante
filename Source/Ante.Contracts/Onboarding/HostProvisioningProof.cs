// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Ante.Contracts.Onboarding;

/// <summary>
/// Represents the host-signed proof carried by every host provisioning outcome: a compact JWS signed with one of
/// the host keys pinned in Ante's Ante:HostOutcomes:Keys. Its claims repeat the event type, operation,
/// attempt, host, outcome and reason code, so Ante can refuse an outcome that was not signed for exactly this
/// delivery. See the contracts documentation for the claim set.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record HostProvisioningProof(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="HostProvisioningProof"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator HostProvisioningProof(string value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="HostProvisioningProof"/> to its underlying <see cref="string"/> value.
    /// </summary>
    /// <param name="proof">The value to convert from.</param>
    public static implicit operator string(HostProvisioningProof proof) => proof.Value;
}
