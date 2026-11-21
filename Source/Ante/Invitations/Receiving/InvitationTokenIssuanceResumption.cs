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
    /// <param name="cancellationToken">Stops waiting for Chronicle; the client calls take no token, so a call already sent is abandoned, not aborted.</param>
    /// <returns>How many invitations were resumed.</returns>
    public async Task<int> ResumeAll(CancellationToken cancellationToken = default)
    {
        var resumed = 0;
        foreach (var awaiting in await eventStore.ReadModels.GetInstances<InvitationAwaitingSigningKey>().WaitAsync(cancellationToken))
        {
            // The log decides, not the read model: it may lag a resumption another instance already recorded.
            var history = await eventStore.EventLog.GetForEventSourceIdAndEventTypes(awaiting.Id, _types).WaitAsync(cancellationToken);
            if (history.Count == 0 || history[^1].Content is not InvitationTokenIssuanceDeferred deferred)
            {
                continue;
            }

            // A concurrent resumption, or a newer deferral, wins; this invitation is looked at again next pass.
            var result = await eventStore.EventLog.Append(
                awaiting.Id,
                new InvitationTokenIssuanceResumed(deferred.TriggerSequenceNumber),
                concurrencyScope: new(history[^1].Context.SequenceNumber, awaiting.Id, EventTypes: _types)).WaitAsync(cancellationToken);
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
/// Passes are best-effort: each is bounded by <see cref="PassTimeout"/>, a failed or timed-out pass is logged and
/// retried on the next tick, and no readiness check depends on a pass - see <see cref="AnteHealthChecks"/>.
/// </remarks>
public class InvitationTokenIssuanceResumption : BackgroundService
{
    /// <summary>
    /// Gets the time between passes.
    /// </summary>
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Gets how long one pass may wait for Chronicle before it is abandoned and retried on the next tick.
    /// </summary>
    public static readonly TimeSpan PassTimeout = TimeSpan.FromSeconds(10);

    readonly IServiceScopeFactory _scopes;
    readonly IOptions<InvitationTokenConfig> _config;
    readonly ILogger<InvitationTokenIssuanceResumption> _logger;
    readonly TimeSpan _interval;
    readonly TimeSpan _passTimeout;

    /// <summary>
    /// Initializes a new instance of the <see cref="InvitationTokenIssuanceResumption"/> class.
    /// </summary>
    /// <param name="scopes">Creates a scope for each pass.</param>
    /// <param name="config">The signing configuration.</param>
    /// <param name="logger">Logs resumed invitations and failed passes.</param>
    public InvitationTokenIssuanceResumption(
        IServiceScopeFactory scopes,
        IOptions<InvitationTokenConfig> config,
        ILogger<InvitationTokenIssuanceResumption> logger)
        : this(scopes, config, logger, Interval, PassTimeout)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvitationTokenIssuanceResumption"/> class with explicit timing.
    /// </summary>
    /// <param name="scopes">Creates a scope for each pass.</param>
    /// <param name="config">The signing configuration.</param>
    /// <param name="logger">Logs resumed invitations and failed passes.</param>
    /// <param name="interval">The time between passes.</param>
    /// <param name="passTimeout">How long one pass may wait for Chronicle.</param>
    internal InvitationTokenIssuanceResumption(
        IServiceScopeFactory scopes,
        IOptions<InvitationTokenConfig> config,
        ILogger<InvitationTokenIssuanceResumption> logger,
        TimeSpan interval,
        TimeSpan passTimeout)
    {
        _scopes = scopes;
        _config = config;
        _logger = logger;
        _interval = interval;
        _passTimeout = passTimeout;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // BackgroundService.StartAsync runs the synchronous prefix of ExecuteAsync on the host startup path; yielding
        // keeps every pass - including the first - off startup and readiness.
        await Task.Yield();
        if (!InvitationSigningKey.IsConfigured(_config.Value))
        {
            return;
        }

        using var timer = new PeriodicTimer(_interval);
        try
        {
            do
            {
                await Pass(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    async Task Pass(CancellationToken stoppingToken)
    {
        using var bound = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        bound.CancelAfter(_passTimeout);
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var resumed = await scope.ServiceProvider.GetRequiredService<InvitationTokenIssuanceResumer>().ResumeAll(bound.Token);
            if (resumed > 0)
            {
                _logger.LogResumed(resumed);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (OperationCanceledException)
        {
            _logger.LogResumptionTimedOut(_passTimeout);
        }
        catch (Exception error)
        {
            _logger.LogResumptionFailed(error);
        }
    }
}
