// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Contracts.Invitations;

/// <summary>
/// Represents the type of invitation flow a user is going through.
/// </summary>
public enum InvitationFlowType
{
    /// <summary>
    /// The user is being invited to join an existing tenant.
    /// </summary>
    JoinTenant = 0,

    /// <summary>
    /// The user is being invited to create a new tenant.
    /// </summary>
    CreateTenant = 1,
}
