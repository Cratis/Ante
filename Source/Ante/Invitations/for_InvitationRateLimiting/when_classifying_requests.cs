// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.for_InvitationRateLimiting;

public class when_classifying_requests : Specification
{
    [Theory]
    [InlineData("/_invite/stage", true)]
    [InlineData("/_invite/exchange", true)]
    [InlineData("/_INVITE/Exchange", true)]
    [InlineData("/api/invitations/user-setup", true)]
    [InlineData("/api/invitations/user-setup/status-for-invitation", true)]
    [InlineData("/api/invitations/organization-setup/status-for-invitation", true)]
    [InlineData("/api/invitations/receiving/pending-join-for-current-invitee", true)]
    [InlineData("/api/invitations/host-outcome/for-attempt", true)]
    [InlineData("/api/organization/registration/start", false)]
    [InlineData("/api/invitations-other", false)]
    [InlineData("/_invited", false)]
    [InlineData("/api/locale-config", false)]
    [InlineData("/healthz/ready", false)]
    void should_only_limit_invitation_endpoints(string path, bool limited) =>
        Assert.Equal(limited, InvitationRateLimiting.IsInvitationRequest(path));
}
#endif
