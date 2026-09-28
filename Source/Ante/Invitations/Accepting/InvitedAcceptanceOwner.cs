// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Organization.Registration;

namespace Ante.Invitations.Accepting;

/// <summary>
/// The exact canonical actor recorded in the same local append as an invited acceptance.
/// Never forwarded to the host; the published provider name is a separate login-routing value.
/// </summary>
/// <param name="LobbyScope">The authenticated lobby scope.</param>
/// <param name="ProviderKey">The canonical registration key, not its display name.</param>
/// <param name="ProviderIssuer">The canonical authority.</param>
/// <param name="OwnerSubject">The case-sensitive subject and compliance owner.</param>
[EventType]
public record InvitedAcceptanceOwnerRecorded(
    string LobbyScope,
    string ProviderKey,
    string ProviderIssuer,
    [property: Subject] RegistrationOwnerSubject OwnerSubject);
