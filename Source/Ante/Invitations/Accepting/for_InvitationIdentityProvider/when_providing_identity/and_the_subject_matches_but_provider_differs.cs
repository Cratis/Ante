// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Ante.IdentityProviders;
using Ante.Invitations.for_query_access;

namespace Ante.Invitations.Accepting.for_InvitationIdentityProvider.when_providing_identity;

public class and_the_subject_matches_but_provider_differs : Specification
{
    IdentityDetails _result = null!;

    async Task Because()
    {
        var session = new AcceptedInvitation("shared-subject", "github", InvitationId.New(), InvitationFlowType.CreateTenant, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30));
        var resolver = Substitute.For<IIdentityProviderResolver>();
        resolver.ResolveFrom(Arg.Any<IEnumerable<string?>>()).Returns("other");
        var provider = new InvitationIdentityProvider(QueryCollections.With(session), resolver);
        _result = await provider.Provide(new IdentityProviderContext(
            "shared-subject",
            "identity-name",
            [new(ClaimTypes.NameIdentifier, "shared-subject"), new("iss", "other")]));
    }

    [Fact] void should_not_reveal_the_other_providers_invitation_id() =>
        Assert.Equal(InvitationId.NotSet, ((InvitationIdentityDetails)_result.Details).InvitationId);
}
#endif
