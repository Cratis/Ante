// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.UserSetup.for_UserSetupAcceptanceStatusView.given;

namespace Ante.Invitations.UserSetup.for_UserSetupAcceptanceStatusView.when_getting_status;

public class and_the_read_models_lag_behind_a_published_acceptance : a_join_status_query_with_lagging_read_models
{
    UserSetupAcceptanceStatus _result;

    void Establish()
    {
        Local.AddRange([Accepted(), Legal()]);
        Outbox.AddRange([Accepted(), Legal()]);
    }

    void Because() => _result = Seeded();

    [Fact] void should_seed_accepted() => _result.ShouldEqual(UserSetupAcceptanceStatus.Accepted);
}
#endif
