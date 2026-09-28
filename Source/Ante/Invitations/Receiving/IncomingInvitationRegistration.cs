// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.Receiving;

/// <summary>
/// Registers the configured host inboxes without holding up application startup when Chronicle is unavailable.
/// </summary>
/// <param name="subscriptions">The runtime inbox registration.</param>
/// <param name="options">The validated routing configuration.</param>
/// <param name="logger">Logs failed attempts.</param>
public class IncomingInvitationRegistration(
    IncomingInvitationSubscriptions subscriptions,
    IOptions<AnteOptions> options,
    ILogger<IncomingInvitationRegistration> logger) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // BackgroundService.StartAsync runs the synchronous prefix of ExecuteAsync on the host startup path.
        await Task.Yield();
        var failures = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await subscriptions.Initialize(options.Value, stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogIncomingInvitationRegistrationFailed(ex);
                failures++;
            }

            var delay = TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, Math.Min(failures - 1, 5))));
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }
}
