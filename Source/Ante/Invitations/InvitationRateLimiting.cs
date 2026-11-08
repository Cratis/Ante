// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Ante.Invitations;

/// <summary>
/// Classifies the invitation endpoints that <see cref="InvitationOptions.RequestsPerMinutePerClient"/> limits.
/// </summary>
public static class InvitationRateLimiting
{
    /// <summary>
    /// The route prefix every invitation command and query (setup, status and pending reads) shares.
    /// </summary>
    public const string ApiRoutePrefix = "/api/invitations";

    /// <summary>
    /// The route prefix of the host-to-Ante stage and exchange endpoints.
    /// </summary>
    public const string ExchangeRoutePrefix = "/_invite";

    /// <summary>
    /// The route of the Arc observable-query WebSocket hub. <c language="csharp">UseCratisArc</c> maps it, and the observable
    /// invitation status queries (<c language="csharp">StatusForInvitation</c>) subscribe through it instead of their own route.
    /// </summary>
    public const string QueryWebSocketRoute = "/.cratis/queries/ws";

    /// <summary>
    /// The route of the Arc observable-query Server-Sent Events hub, including its subscribe and unsubscribe calls.
    /// The sibling <c language="csharp">/.cratis/queries/health</c> probe is deliberately not limited.
    /// </summary>
    public const string QueryServerSentEventsRoute = "/.cratis/queries/sse";

    /// <summary>
    /// Determines whether a request targets an invitation endpoint.
    /// </summary>
    /// <param name="path">The request path.</param>
    /// <returns>True for the stage and exchange endpoints, the invitation commands and queries, and the observable-query hubs.</returns>
    public static bool IsInvitationRequest(PathString path) =>
        path.StartsWithSegments(ApiRoutePrefix, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments(ExchangeRoutePrefix, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments(QueryWebSocketRoute, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments(QueryServerSentEventsRoute, StringComparison.OrdinalIgnoreCase);
}
