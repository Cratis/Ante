// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Invitations;

/// <summary>The exception thrown for invalid invitation presentation configuration.</summary>
/// <param name="message">A diagnostic identifying the invalid setting.</param>
public class InvitationContentConfigurationInvalid(string message) : Exception(message);
