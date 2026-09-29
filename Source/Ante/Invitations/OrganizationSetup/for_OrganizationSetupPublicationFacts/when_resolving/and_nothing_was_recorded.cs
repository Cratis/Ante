// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.OrganizationSetup.for_OrganizationSetupPublicationFacts.given;
using Ante.Outbox;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupPublicationFacts.when_resolving;

public class and_nothing_was_recorded : an_organization_flow_with_lagging_read_models
{
    OrganizationSetupFacts _result = null!;

    async Task Because() => _result = await Facts.Resolve(Id);

    [Fact] void should_have_no_progress() => _result.Progress.ShouldEqual(PublicationProgress.None);
    [Fact] void should_have_no_organization_name() => _result.OrganizationName.ShouldBeNull();
}
#endif
