// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.UserSetup.for_JoinTenantPublicationFacts.given;
using Ante.Outbox;

namespace Ante.Invitations.UserSetup.for_JoinTenantPublicationFacts.when_resolving;

public class and_nothing_was_recorded : a_join_flow_with_lagging_read_models
{
    PublicationProgress _result;

    async Task Because() => _result = await Facts.Resolve(Id);

    [Fact] void should_have_no_progress() => _result.ShouldEqual(PublicationProgress.None);
}
#endif
