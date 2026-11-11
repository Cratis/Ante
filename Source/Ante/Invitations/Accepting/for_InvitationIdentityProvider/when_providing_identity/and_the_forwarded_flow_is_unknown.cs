// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.IdentityProviders;
using Ante.Invitations.Issuing;
using Microsoft.IdentityModel.JsonWebTokens;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_InvitationIdentityProvider.when_providing_identity;

public class and_the_forwarded_flow_is_unknown : Specification
{
    readonly InvitationId _invitationId = InvitationId.New();
    IdentityDetails _result = null!;

    async Task Because()
    {
        var provider = new InvitationIdentityProvider(Substitute.For<IMongoCollection<AcceptedInvitation>>(), Substitute.For<IIdentityProviderResolver>());
        _result = await provider.Provide(new IdentityProviderContext("identity-id", "identity-name", [
            new(JwtRegisteredClaimNames.Jti, _invitationId.ToString()),
            new(InvitationClaims.InvitationType, "unknown-flow"),
        ]));
    }

    [Fact] void should_keep_the_forwarded_invitation() => ((InvitationIdentityDetails)_result.Details).InvitationId.ShouldEqual(_invitationId);
    [Fact] void should_default_to_joining_a_tenant() => ((InvitationIdentityDetails)_result.Details).FlowType.ShouldEqual(InvitationFlowType.JoinTenant);
}
#endif
