// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.IdentityProviders;
using Ante.Invitations.for_query_access;

namespace Ante.Invitations.Accepting.for_InvitationIdentityProvider.when_providing_identity;

public class and_the_canonical_identity_matches_a_live_session : Specification
{
    readonly InvitationId _id = InvitationId.New();
    IdentityDetails _result = null!;

    async Task Because()
    {
        var session = new AcceptedInvitation("canonical-subject", "github", _id, InvitationFlowType.CreateTenant, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30));
        var resolver = Substitute.For<IIdentityProviderResolver>();
        resolver.ResolveFrom(Arg.Any<IEnumerable<string?>>()).Returns("github");
        var provider = new InvitationIdentityProvider(QueryCollections.With(session), resolver);
        _result = await provider.Provide(new IdentityProviderContext(
            "other-subject",
            "identity-name",
            [new("urn:cratis:identity:subject", "canonical-subject"), new("urn:cratis:identity:provider-key", "github")]));
    }

    [Fact] void should_return_only_the_matching_invitation() => Assert.Equal(_id, ((InvitationIdentityDetails)_result.Details).InvitationId);
    [Fact] void should_return_the_exchanged_flow() => Assert.Equal(InvitationFlowType.CreateTenant, ((InvitationIdentityDetails)_result.Details).FlowType);
}
#endif
