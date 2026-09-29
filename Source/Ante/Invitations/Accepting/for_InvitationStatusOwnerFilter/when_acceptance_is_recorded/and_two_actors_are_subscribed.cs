// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Reactive.Linq;
using Ante.Invitations.OrganizationSetup;
using Ante.Invitations.UserSetup;
using Ante.Organization.Registration;
using Ante.Outbox;
using Cratis.Chronicle.EventSequences;

namespace Ante.Invitations.Accepting.for_InvitationStatusOwnerFilter.when_acceptance_is_recorded;

public class and_two_actors_are_subscribed : Specification
{
    readonly InvitationId _id = InvitationId.New();
    readonly InvitedAcceptanceOwnerRecorded _owner = new("lobby", "oidc", "https://identity.example", (RegistrationOwnerSubject)"owner");
    readonly InvitedAcceptanceOwnerRecorded _other = new("lobby", "oidc", "https://identity.example", (RegistrationOwnerSubject)"other");
    readonly List<UserSetupAcceptanceStatus> _ownerUserStatuses = [];
    readonly List<UserSetupAcceptanceStatus> _otherUserStatuses = [];
    readonly List<OrganizationSetupAcceptanceStatus> _ownerOrganizationStatuses = [];
    readonly List<OrganizationSetupAcceptanceStatus> _otherOrganizationStatuses = [];
    readonly IEventStore _store = Substitute.For<IEventStore>();
    bool _committed;

    void Establish()
    {
        var log = Substitute.For<IEventLog>();
        _store.EventLog.Returns(log);
        log.GetForEventSourceIdAndEventTypes(
            Arg.Any<EventSourceId>(),
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventSourceType>())
            .Returns(_ => Task.FromResult<IImmutableList<AppendedEvent>>(_committed
                ? [new AppendedEvent(EventContext.Empty, _owner)] : []));
    }

    void Because()
    {
        using var users = new UserSetupStatusSubscriptions();
        using var organizations = new OrganizationSetupStatusSubscriptions();
        var ownerIdentity = IdentityFor(_owner);
        var otherIdentity = IdentityFor(_other);
        var userFacts = Substitute.For<IJoinTenantPublicationFacts>();
        var organizationFacts = Substitute.For<IOrganizationSetupPublicationFacts>();
        organizationFacts.Resolve(Arg.Any<InvitationId>(), Arg.Any<OrganizationSetupProgress?>())
            .Returns(new OrganizationSetupFacts(PublicationProgress.None, null));
        using var ownerUser = UserSetupAcceptanceStatusView.StatusForInvitation(
            _id, ownerIdentity, users, userFacts, _store)
            .Subscribe(view => _ownerUserStatuses.Add(view.Status));
        using var otherUser = UserSetupAcceptanceStatusView.StatusForInvitation(
            _id, otherIdentity, users, userFacts, _store)
            .Subscribe(view => _otherUserStatuses.Add(view.Status));
        using var ownerOrganization = OrganizationSetupAcceptanceStatusView.StatusForInvitation(
            _id, ownerIdentity, organizations, organizationFacts, _store)
            .Subscribe(view => _ownerOrganizationStatuses.Add(view.Status));
        using var otherOrganization = OrganizationSetupAcceptanceStatusView.StatusForInvitation(
            _id, otherIdentity, organizations, organizationFacts, _store)
            .Subscribe(view => _otherOrganizationStatuses.Add(view.Status));

        _committed = true;
        users.MarkRecorded(_id);
        organizations.MarkRecorded(_id, "Acme");
        users.MarkAccepted(_id);
        organizations.MarkAccepted(_id, "Acme");
    }

    static ISignedInIdentity IdentityFor(InvitedAcceptanceOwnerRecorded actor)
    {
        var identity = Substitute.For<ISignedInIdentity>();
        identity.IsAttestedExchange.Returns(true);
        identity.IsVerifiedRecoveryOwnerOf(Arg.Any<InvitationId>(), Arg.Any<IEventStore>()).Returns(true);
        identity.CaptureRecoveryActor().Returns(actor);
        return identity;
    }

    [Fact] void should_deliver_private_user_status_only_to_the_accepting_actor() => Assert.Equal(
        [UserSetupAcceptanceStatus.Pending, UserSetupAcceptanceStatus.Recorded, UserSetupAcceptanceStatus.Accepted], _ownerUserStatuses);
    [Fact] void should_keep_the_other_user_subscription_pending() => Assert.Equal([UserSetupAcceptanceStatus.Pending], _otherUserStatuses);
    [Fact] void should_deliver_private_organization_status_only_to_the_accepting_actor() => Assert.Equal(
        [OrganizationSetupAcceptanceStatus.Pending, OrganizationSetupAcceptanceStatus.Recorded, OrganizationSetupAcceptanceStatus.Accepted], _ownerOrganizationStatuses);
    [Fact] void should_keep_the_other_organization_subscription_pending() => Assert.Equal([OrganizationSetupAcceptanceStatus.Pending], _otherOrganizationStatuses);
}
#endif
