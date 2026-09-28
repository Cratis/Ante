// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Ante.Integration.given;

/// <summary>
/// Bounded polling for effects that cross asynchronous observer boundaries (outbox to inbox, reactors,
/// projections). Fails with a timeout rather than passing on an absent effect.
/// </summary>
public static class Eventually
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    static readonly TimeSpan _interval = TimeSpan.FromMilliseconds(200);

    public static async Task<T> Get<T>(Func<Task<T?>> probe, TimeSpan? timeout = default, string? what = default)
    {
        var deadline = DateTimeOffset.UtcNow + (timeout ?? DefaultTimeout);
        while (true)
        {
            var value = await probe();
            if (value is not null)
            {
                return value;
            }

            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException($"Timed out after {timeout ?? DefaultTimeout} waiting for {what ?? typeof(T).Name}.");
            }

            await Task.Delay(_interval);
        }
    }

    public static async Task Until(Func<Task<bool>> condition, TimeSpan? timeout = default, string? what = default) =>
        await Get<object>(async () => await condition() ? true : null, timeout, what);
}
