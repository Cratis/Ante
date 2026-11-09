// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Ante.Contracts.Onboarding;

/// <summary>
/// Represents the host's identifier for one provisioning attempt of an accepted onboarding operation. The host
/// chooses it; the first verified host outcome for an operation binds it, and outcomes naming another attempt
/// for that operation are refused.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record HostProvisioningAttemptId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Implicitly converts a <see cref="Guid"/> to a <see cref="HostProvisioningAttemptId"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator HostProvisioningAttemptId(Guid value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="HostProvisioningAttemptId"/> to its underlying <see cref="Guid"/> value.
    /// </summary>
    /// <param name="attempt">The value to convert from.</param>
    public static implicit operator Guid(HostProvisioningAttemptId attempt) => attempt.Value;
}
