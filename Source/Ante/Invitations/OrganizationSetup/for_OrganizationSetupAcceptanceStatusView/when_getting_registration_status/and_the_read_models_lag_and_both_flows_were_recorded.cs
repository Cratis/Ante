// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.OrganizationSetup.for_OrganizationSetupAcceptanceStatusView.given;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupAcceptanceStatusView.when_getting_registration_status;

public class and_the_read_models_lag_and_both_flows_were_recorded : an_organization_status_query_with_lagging_read_models
{
    OrganizationSetupAcceptanceStatusView _result = null!;

    void Establish()
    {
        Local.AddRange([Invited(), Registered()]);
        Outbox.AddRange([Invited(), Registered()]);
    }

    async Task Because() => _result = await SeededForRegistration();

    [Fact] void should_seed_recorded_from_the_projected_record() => _result.Status.ShouldEqual(OrganizationSetupAcceptanceStatus.Recorded);
}
#endif
