// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.OrganizationSetup.for_OrganizationSetupPublicationFacts.given;
using Ante.Outbox;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupPublicationFacts.when_resolving;

public class and_a_self_registration_was_published : an_organization_flow_with_lagging_read_models
{
    OrganizationSetupFacts _result = null!;

    void Establish()
    {
        Local.Add(Registered());
        Outbox.Add(Registered());
    }

    async Task Because() => _result = await Facts.Resolve(Id);

    [Fact] void should_be_published() => _result.Progress.ShouldEqual(PublicationProgress.Published);
    [Fact] void should_carry_the_registered_organization_name() => _result.OrganizationName!.Value.ShouldEqual("Registered Org");
}
#endif
