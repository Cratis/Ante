// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Microsoft.Extensions.Logging;

namespace Ante.Invitations.Receiving.for_IncomingInvitationReactor.when_an_invitation_arrives.given;

/// <summary>
/// Open generic fixture: ReactorScenario exercises the existing typed decisions without registering
/// another discovered incoming reactor in the application (discovery excludes generic types).
/// </summary>
/// <typeparam name="TMarker">A type marker that keeps this test fixture generic and undiscoverable.</typeparam>
/// <param name="store">The substituted event store.</param>
/// <param name="logger">The handler logger.</param>
public class IncomingInvitationScenarioReactor<TMarker>(IEventStore store, ILogger<IncomingInvitationReactor> logger)
    : IncomingInvitationReactor(store, logger, IncomingInvitationTestOptions.Legacy), IReactor;
#endif
