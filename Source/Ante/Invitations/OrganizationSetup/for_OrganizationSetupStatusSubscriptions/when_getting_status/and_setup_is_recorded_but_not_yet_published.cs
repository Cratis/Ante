// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupStatusSubscriptions.when_getting_status;

/// <summary>
/// A client that reconnects after submitting - but before publication reached the outbox - must see
/// Recorded, not Pending: Pending would tell the wizard it is safe to resubmit, which would collide with
/// the one-use invitation constraint (or duplicate an organization name claim for self-service
/// registration).
/// </summary>
public class and_setup_is_recorded_but_not_yet_published : Specification
{
    static readonly InvitationId _invitationId = InvitationId.New();

    OrganizationSetupStatusSubscriptions _subscriptions = null!;
    OrganizationSetupAcceptanceStatusView _result = null!;

    void Establish() => _subscriptions = new();

    void Because() => _result = ((BehaviorSubject<OrganizationSetupAcceptanceStatusView>)_subscriptions.GetStatus(_invitationId, "Acme", isRecorded: true, isFullyPublished: false)).Value;

    [Fact] void should_be_recorded() => Assert.Equal(OrganizationSetupAcceptanceStatus.Recorded, _result.Status);
    [Fact] void should_carry_the_organization_name() => Assert.Equal((TenantName)"Acme", _result.OrganizationName);
}
#endif
