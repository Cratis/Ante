// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Organization.Registration.for_RegistrationRateLimiting.when_limiting_by_client;

public class and_invitation_requests_use_different_routes : a_limiter
{
    int _permitted;

    void Establish() => _options.Invitations.RequestsPerMinutePerClient = 3;

    void Because() => _permitted = new[]
    {
        "/_invite/stage",
        Exchange,
        "/api/invitations/user-setup/status-for-invitation",
        "/api/invitations/receiving/pending-join-for-current-invitee",
        "/.cratis/queries/ws",
        "/.cratis/queries/sse/subscribe",
    }.Count(path => Permitted(() => Request(path), 1) == 1);

    [Fact] void should_count_them_in_one_budget() => Assert.Equal(3, _permitted);
}
#endif
