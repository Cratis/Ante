// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.OrganizationSetup.for_OrganizationSetupAcceptanceStatusView.given;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupAcceptanceStatusView.when_getting_invitation_status;

public class and_the_read_models_lag_behind_a_recorded_setup : an_organization_status_query_with_lagging_read_models
{
    OrganizationSetupAcceptanceStatusView _result = null!;

    void Establish() => Local.Add(Invited());

    async Task Because() => _result = await Task.FromResult(SeededForInvitation());

    [Fact] void should_seed_recorded() => _result.Status.ShouldEqual(OrganizationSetupAcceptanceStatus.Recorded);
    [Fact] void should_seed_the_organization_name() => _result.OrganizationName.Value.ShouldEqual("Invited Org");
}
#endif
