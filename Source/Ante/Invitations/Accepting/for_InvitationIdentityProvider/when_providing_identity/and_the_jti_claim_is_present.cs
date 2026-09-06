// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Issuing;
using Cratis.Arc.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using MongoDB.Driver;

namespace Ante.Invitations.Accepting.for_InvitationIdentityProvider.when_providing_identity;

public class and_the_jti_claim_is_present : Specification
{
    static readonly Guid _invitationId = Guid.NewGuid();

    InvitationIdentityProvider _provider = null!;
    IdentityDetails _result = null!;

    void Establish() =>
        _provider = new(Substitute.For<IMongoCollection<AcceptedInvitation>>());

    async Task Because() =>
        _result = await _provider.Provide(new IdentityProviderContext(
            "identity-id",
            "identity-name",
            [
                new(JwtRegisteredClaimNames.Jti, _invitationId.ToString()),
                new(InvitationClaims.InvitationType, nameof(InvitationFlowType.CreateTenant)),
            ]));

    [Fact] void should_be_authorized() => Assert.True(_result.IsUserAuthorized);

    [Fact]
    void should_carry_the_invitation_id_and_flow_type()
    {
        var details = (InvitationIdentityDetails)_result.Details;
        Assert.Equal((InvitationId)_invitationId, details.InvitationId);
        Assert.Equal(InvitationFlowType.CreateTenant, details.FlowType);
    }
}
#endif
