// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.for_query_access;
using Ante.Invitations.OrganizationSetup.for_OrganizationSetupAcceptanceStatusView.given;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupAcceptanceStatusView.when_getting_invitation_status;

public class and_the_read_models_lag_and_both_flows_were_recorded_with_a_projected_record : an_organization_status_query_with_lagging_read_models
{
    OrganizationSetupAcceptanceStatusView _result = null!;

    void Establish()
    {
        Local.AddRange([Invited(), Registered()]);
        Outbox.AddRange([Invited(), Registered()]);
        Facts = new(
            QueryCollections.With(new OrganizationSetupProgress(Id, "Projected Org")),
            QueryCollections.WithMany<OrganizationSetupPublished>(),
            Store);
    }

    void Because() => _result = SeededForInvitation();

    [Fact] void should_seed_recorded_rather_than_safe_to_resubmit() => _result.Status.ShouldEqual(OrganizationSetupAcceptanceStatus.Recorded);
    [Fact] void should_seed_the_projected_organization_name() => _result.OrganizationName.Value.ShouldEqual("Projected Org");
}
#endif
