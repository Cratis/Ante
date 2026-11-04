// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Contracts;

/// <summary>
/// The named tags that address Ante's channel between an operator (the host that invites) and a lobby.
/// </summary>
/// <remarks>
/// A host addresses a request to one lobby with the Chronicle <c language="csharp">audience</c> tag
/// (<c language="csharp">WellKnownTags.Audience</c>), which the lobby's subscription filters on, and names itself with
/// <see cref="From"/>. The lobby replies on its outbox with <c language="csharp">audience=&lt;from&gt;</c>, so the
/// operator's subscription to that lobby receives exactly the answers to its own requests. Nothing here changes an event
/// schema or marks an event type as requiring an audience.
/// </remarks>
public static class AudienceTags
{
    /// <summary>
    /// The tag name a request carries to identify the operator that sent it; the lobby addresses its replies to this value.
    /// </summary>
    public const string From = "from";
}
