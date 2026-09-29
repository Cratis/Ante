// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Invitations.Issuing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting;

/// <summary>
/// Prepares the storage the exchange depends on - its indexes and the token upgrade window - in the background,
/// so a temporarily unavailable MongoDB does not stop the host from starting. Connection and server-transition
/// failures are retried; readiness (and with it every exchange write) stays closed until the work has succeeded.
/// A rejected command, such as a conflicting index definition, is logged with its server code and fails the host.
/// </summary>
/// <param name="scopes">Creates a scope for the tenant-aware collections.</param>
/// <param name="logger">Reports failures.</param>
/// <param name="exchangeConfig">Determines which exchange paths require additional indexes.</param>
/// <param name="upgradeWindow">The token upgrade window to load once MongoDB is reachable, when one is registered.</param>
public sealed class AcceptedInvitationIndexRegistration(
    IServiceScopeFactory scopes,
    ILogger<AcceptedInvitationIndexRegistration> logger,
    IOptions<InvitationExchangeConfig>? exchangeConfig = null,
    DeferredInvitationTokenUpgradeWindow? upgradeWindow = null) : BackgroundService, IExchangeIndexReadiness
{
    volatile bool _isReady;

    /// <inheritdoc/>
    public bool IsReady => _isReady;

    /// <summary>Retries connection and server-transition failures, but not arbitrary command rejections.</summary>
    /// <param name="exception">The installation failure.</param>
    /// <returns>Whether the connection or server state can be retried.</returns>
    internal static bool IsRetryable(Exception exception) =>
        exception is TimeoutException or MongoNotPrimaryException or MongoNodeIsRecoveringException ||
        (exception is MongoException && exception is not MongoCommandException);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var acceptedInvitations = scope.ServiceProvider.GetRequiredService<IMongoCollection<AcceptedInvitation>>();
                await AcceptedInvitationIndexes.EnsureCreated(acceptedInvitations, stoppingToken);
                if (exchangeConfig?.Value.Mode == InvitationExchangeMode.Attested)
                {
                    await StagedInvitationTransactionIndexes.EnsureCreated(
                        scope.ServiceProvider.GetRequiredService<IMongoCollection<StagedInvitationTransaction>>(), stoppingToken);
                    await AttestedInvitationSessionIndexes.EnsureCreated(
                        scope.ServiceProvider.GetRequiredService<IMongoCollection<AttestedInvitationSession>>(), stoppingToken);
                }

                if (upgradeWindow is not null)
                {
                    await upgradeWindow.Load(acceptedInvitations.Database, stoppingToken);
                }

                _isReady = true;
                logger.LogAcceptedInvitationIndexesReady();
                return;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested && IsRetryable(ex))
            {
                logger.LogAcceptedInvitationIndexesUnavailable(ex);

                // Infrastructure connection backoff, not an observer or application-state wait.
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            catch (MongoCommandException ex) when (!stoppingToken.IsCancellationRequested)
            {
                // A command rejection (conflicting definition, authorization, ...) will not heal by retrying.
                logger.LogAcceptedInvitationIndexesRejected(ex.Code, ex.CodeName ?? string.Empty, ex);
                throw;
            }
        }
    }
}
