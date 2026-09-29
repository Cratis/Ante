// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.OrganizationSetup.for_OrganizationSetupAcceptanceStatusView.given;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupAcceptanceStatusView.when_getting_registration_status;

public class and_the_outbox_read_model_lags_behind_a_recorded_registration : an_organization_status_query_with_lagging_read_models
{
    OrganizationSetupAcceptanceStatusView _result = null!;

    void Establish() => Local.Add(Registered());

    async Task Because() => _result = await SeededForRegistration();

    [Fact] void should_seed_recorded() => _result.Status.ShouldEqual(OrganizationSetupAcceptanceStatus.Recorded);
}
#endif
