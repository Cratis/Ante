// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.for_query_access;
using Ante.Invitations.UserSetup.for_JoinTenantPublicationFacts.given;
using Ante.Outbox;

namespace Ante.Invitations.UserSetup.for_JoinTenantPublicationFacts.when_resolving;

public class and_only_the_read_model_shows_a_record : a_join_flow_with_lagging_read_models
{
    PublicationProgress _result;

    void Establish() => Facts = new(
        QueryCollections.With(new UserSetupProgress(Id, AcceptanceRecorded: true)),
        QueryCollections.WithMany<JoinTenantAcceptancePublished>(),
        Store);

    async Task Because() => _result = await Facts.Resolve(Id);

    [Fact] void should_be_recorded() => _result.ShouldEqual(PublicationProgress.Recorded);
}
#endif
