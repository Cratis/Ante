// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Ante.Resources;

namespace Ante.Locale;

/// <summary>
/// Translates Chronicle's append-time constraint and concurrency messages at the Arc HTTP response boundary.
/// Chronicle 19.4.7 captures the message in the constraint definition/attribute at startup,
/// outside any request culture; the stable reasonDetail constraint name identifies the failure.
/// </summary>
public static class LocalizedConstraintResponses
{
    static readonly Dictionary<string, string> _messages = new(StringComparer.Ordinal)
    {
        ["UniqueOrganizationName"] = "OrganizationNameExists",
        ["OneUseJoinTenantInvitation"] = "InvitationAlreadyAccepted",
        ["OneUseCreateTenantInvitation"] = "InvitationAlreadyAccepted",
        ["OneUseOnboardingAttempt"] = "AttemptAlreadySubmitted"
    };

    /// <summary>
    /// Replaces only known Chronicle constraint messages; other response content is untouched.
    /// </summary>
    /// <param name="json">The Arc response body.</param>
    /// <returns>Localized JSON or the original body.</returns>
    public static string Translate(string json)
    {
        try
        {
            var root = JsonNode.Parse(json);
            if (root?["validationResults"] is not JsonArray results)
            {
                return json;
            }

            var changed = false;
            foreach (var result in results.OfType<JsonObject>())
            {
                // A concurrency violation means the state the command decided on moved before it was
                // appended - for onboarding, typically a newer legal document set. Chronicle's message is
                // technical and fixed; the user needs a localized "review and submit again".
                if (result["reason"]?.GetValue<string>() == "concurrencyViolation")
                {
                    result["message"] = Messages.Get("ConcurrentChange");
                    changed = true;
                    continue;
                }

                if (result["reason"]?.GetValue<string>() != "constraintViolation" ||
                    result["reasonDetail"]?.GetValue<string>() is not { } name ||
                    !_messages.TryGetValue(name, out var key))
                {
                    continue;
                }

                result["message"] = Messages.Get(key);
                changed = true;
            }

            return changed ? root.ToJsonString() : json;
        }
        catch (System.Text.Json.JsonException)
        {
            return json;
        }
    }

    /// <summary>
    /// Buffers command POST results only; observable queries and other streaming responses pass through.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="next">The next middleware.</param>
    /// <returns>The asynchronous operation.</returns>
    public static async Task Invoke(HttpContext context, RequestDelegate next)
    {
        if (!HttpMethods.IsPost(context.Request.Method) || !context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        var original = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;
        try
        {
            await next(context);
            buffer.Position = 0;
            if (context.Response.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
            {
                using var reader = new StreamReader(buffer, leaveOpen: true);
                var body = Translate(await reader.ReadToEndAsync());
                var bytes = System.Text.Encoding.UTF8.GetBytes(body);
                context.Response.ContentLength = bytes.Length;
                await original.WriteAsync(bytes);
            }
            else
            {
                await buffer.CopyToAsync(original);
            }
        }
        finally
        {
            context.Response.Body = original;
        }
    }
}
