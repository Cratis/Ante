// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Invitations.Issuing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Ante.Invitations.Receiving;

/// <summary>
/// Resumes every invitation waiting for a signing key by recording <see cref="InvitationTokenIssuanceResumed"/>, which
/// <see cref="InvitationTokenIssuingReactor"/> then issues the token for.
/// </summary>
/// <param name="eventStore">The event store holding the waiting invitations.</param>
public class InvitationTokenIssuanceResumer(IEventStore eventStore)
{
    static readonly EventType[] _types =
    [
        typeof(InvitationTokenIssuanceDeferred).GetEventType(),
        typeof(InvitationTokenIssuanceResumed).GetEventType(),
    ];

    /// <summary>
    /// Resumes every waiting invitation that is not already resumed.
    /// </summary>
    /// <returns>How many invitations were resumed.</returns>
    public async Task<int> ResumeAll()
    {
        var resumed = 0;
        foreach (var awaiting in await eventStore.ReadModels.GetInstances<InvitationAwaitingSigningKey>())
        {
            // The log decides, not the read model: it may lag a resumption another instance already recorded.
            var history = await eventStore.EventLog.GetForEventSourceIdAndEventTypes(awaiting.Id, _types);
            if (history.Count == 0 || history[^1].Content is not InvitationTokenIssuanceDeferred deferred)
            {
                continue;
            }

            // A concurrent resumption, or a newer deferral, wins; this invitation is looked at again next pass.
            var result = await eventStore.EventLog.Append(
                awaiting.Id,
                new InvitationTokenIssuanceResumed(deferred.TriggerSequenceNumber),
                concurrencyScope: new(history[^1].Context.SequenceNumber, awaiting.Id, EventTypes: _types));
            if (result.IsSuccess)
            {
                resumed++;
            }
        }

        return resumed;
    }
}

/// <summary>
/// Issues the tokens of invitations that arrived while no signing key was configured, on an instance that has one.
/// </summary>
/// <remarks>
/// Runs at startup and then periodically, so an invitation deferred by another instance - or not yet projected when
/// this one started - is not left waiting for the next restart. An instance without a key does nothing.
/// </remarks>
/// <param name="scopes">Creates a scope for each pass.</param>
/// <param name="config">The signing configuration.</param>
/// <param name="logger">Logs resumed invitations and failed passes.</param>
public class InvitationTokenIssuanceResumption(
    IServiceScopeFactory scopes,
    IOptions<InvitationTokenConfig> config,
    ILogger<InvitationTokenIssuanceResumption> logger) : BackgroundService
{
    /// <summary>
    /// Gets the time between passes.
    /// </summary>
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // BackgroundService.StartAsync runs the synchronous prefix of ExecuteAsync on the host startup path.
        await Task.Yield();
        if (!InvitationSigningKey.IsConfigured(config.Value))
        {
            return;
        }

        using var timer = new PeriodicTimer(Interval);
        try
        {
            do
            {
                await Pass();
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    async Task Pass()
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var resumed = await scope.ServiceProvider.GetRequiredService<InvitationTokenIssuanceResumer>().ResumeAll();
            if (resumed > 0)
            {
                logger.LogResumed(resumed);
            }
        }
        catch (Exception error)
        {
            logger.LogResumptionFailed(error);
        }
    }
}
