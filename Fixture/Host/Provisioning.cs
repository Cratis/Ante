// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Invitations;
using Ante.Contracts.Onboarding;

namespace Ante.Fixture.Host;

public sealed partial class Provisioning(Invitations invitations, ILogger<Provisioning> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var id in invitations.PendingProvisioning)
            {
                if (!(await invitations.Received(id)).Any(appended => appended.Content is InvitationToJoinTenantAccepted))
                {
                    continue;
                }

                await invitations.Publish(id, new OnboardingProvisioningCompleted());
                invitations.RecordProvisioning(id);
                Provisioned(logger, id);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Provisioned fixture invitation {InvitationId}")]
    static partial void Provisioned(ILogger logger, Guid invitationId);
}
