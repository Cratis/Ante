// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;

namespace Ante.Invitations.Accepting;

/// <summary>
/// Parses AuthProxy's single-field completion body without admitting body-authored identity.
/// </summary>
public static class InvitationCompletionRequestBody
{
    const int MaximumBodyBytes = 512;

    /// <summary>
    /// Reads a bounded request and rejects missing, duplicated, unknown or mistyped properties.
    /// </summary>
    /// <param name="context">The incoming HTTP request.</param>
    /// <returns>The one transaction, or null for any invalid body.</returns>
    public static async Task<CompleteInvitationRequest?> Read(HttpContext context)
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
            if (properties.Length != 1 || !properties[0].NameEquals("invitationTransaction") ||
                properties[0].Value.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var transaction = properties[0].Value.GetString();
            return string.IsNullOrWhiteSpace(transaction) || transaction.Length > 256
                ? null : new CompleteInvitationRequest(transaction);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
