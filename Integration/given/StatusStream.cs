// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Ante.Integration.given;

/// <summary>One real Arc SSE subscription, bound to the lifetime of its Ante instance.</summary>
public sealed class StatusStream(HttpClient client, HttpResponseMessage response) : IAsyncDisposable
{
    readonly StreamReader _reader = new(response.Content.ReadAsStream());

    /// <summary>Reads the next Arc QueryResult frame under a deadline; EOF is not a terminal status.</summary>
    public async Task<JsonDocument> Next(TimeSpan? timeout = null)
    {
        using var deadline = new CancellationTokenSource(timeout ?? Eventually.DefaultTimeout);
        while (true)
        {
            var line = await _reader.ReadLineAsync(deadline.Token) ?? throw new EndOfStreamException("Arc status subscription closed before its next update.");
            if (line.StartsWith("data: ", StringComparison.Ordinal))
            {
                return JsonDocument.Parse(line[6..]);
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        _reader.Dispose();
        response.Dispose();
        client.Dispose();
        return ValueTask.CompletedTask;
    }
}
