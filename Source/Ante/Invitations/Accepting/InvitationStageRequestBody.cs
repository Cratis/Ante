// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;

namespace Ante.Invitations.Accepting;

/// <summary>
/// Parses only AuthProxy's three-field stage body, including on chunked requests with no Content-Length.
/// </summary>
public static class InvitationStageRequestBody
{
    const int MaximumBodyBytes = 8192;

    /// <summary>
    /// Reads a bounded stage request and rejects unknown, missing, duplicated, or mistyped fields.
    /// </summary>
    /// <param name="context">The HTTP request.</param>
    /// <returns>The parsed request, or null for malformed input.</returns>
    public static async Task<StageInvitationRequest?> Read(HttpContext context)
    {
        var limit = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (limit is { IsReadOnly: false })
        {
            limit.MaxRequestBodySize = MaximumBodyBytes;
        }

        if (context.Request.ContentLength is 0 or > MaximumBodyBytes)
        {
            return null;
        }

        // Read one byte past the bound: a client cannot bypass Content-Length with chunked encoding.
        var data = new byte[MaximumBodyBytes + 1];
        var count = 0;
        while (count < data.Length)
        {
            var read = await context.Request.Body.ReadAsync(data.AsMemory(count), context.RequestAborted);
            if (read == 0)
            {
                break;
            }
            count += read;
        }
        if (count is 0 or > MaximumBodyBytes)
        {
            return null;
        }

        try
        {
            using var json = JsonDocument.Parse(data.AsMemory(0, count));
            if (json.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var properties = json.RootElement.EnumerateObject().ToArray();
            if (properties.Length != 3 ||
                properties.Count(property => property.NameEquals("invitationTransaction")) != 1 ||
                properties.Count(property => property.NameEquals("invitationToken")) != 1 ||
                properties.Count(property => property.NameEquals("invitationChallenge")) != 1 ||
                properties.Any(property => property.Value.ValueKind != JsonValueKind.String))
            {
                return null;
            }

            var transaction = json.RootElement.GetProperty("invitationTransaction").GetString();
            var token = json.RootElement.GetProperty("invitationToken").GetString();
            var challenge = json.RootElement.GetProperty("invitationChallenge").GetString();
            return string.IsNullOrEmpty(transaction) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(challenge)
                ? null
                : new StageInvitationRequest(transaction, token, challenge);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
