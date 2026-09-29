// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.OrganizationSetup.for_OrganizationSetupAcceptanceStatusView.given;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupAcceptanceStatusView.when_getting_invitation_status;

public class and_the_read_models_lag_and_nothing_was_recorded : an_organization_status_query_with_lagging_read_models
{
    OrganizationSetupAcceptanceStatusView _result = null!;

    async Task Because() => _result = await Task.FromResult(SeededForInvitation());

    [Fact] void should_seed_pending() => _result.Status.ShouldEqual(OrganizationSetupAcceptanceStatus.Pending);
}
#endif
