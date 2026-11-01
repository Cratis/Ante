// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Ante.Invitations.Accepting;
using Ante.Invitations.for_query_access;
using Ante.Organization.Registration;
using Ante.Organization.Registration.Review;
using Ante.Outbox;

namespace Ante.Invitations.OrganizationSetup.for_OrganizationSetupAcceptanceStatusView.when_getting_registration_status;

public class and_the_registration_is_held_for_review : Specification
{
    readonly InvitationId _id = InvitationId.New();
    OrganizationSetupAcceptanceStatusView _result = null!;
    IOrganizationSetupPublicationFacts _facts = null!;

    async Task Because()
    {
        var identity = Substitute.For<ISignedInIdentity>();
        identity.IsVerifiedRegistrationOwner(Arg.Any<RegistrationOwner>()).Returns(true);
        var store = QueryCollections.ReadModelStoreWith(new OrganizationSetupProgress(_id, TenantName.NotSet) { OwnerSubject = (RegistrationOwnerSubject)"sub-1", OwnerProvider = "github" });
        var log = Substitute.For<IEventLog>();
        store.EventLog.Returns(log);
        log.GetForEventSourceIdAndEventTypes(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventSourceType>())
            .Returns(Task.FromResult<IImmutableList<AppendedEvent>>(
                [new AppendedEvent(EventContext.Empty, new OrganizationRegistrationHeld("Acme", "sub-1", "github", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", []))]));
        _facts = Substitute.For<IOrganizationSetupPublicationFacts>();
        using var subscriptions = new OrganizationSetupStatusSubscriptions();
        _result = await OrganizationSetupAcceptanceStatusView.StatusForRegistration(_id, identity, subscriptions, store, _facts);
    }

    [Fact] void should_say_it_is_held() => Assert.Equal(OrganizationSetupAcceptanceStatus.Held, _result.Status);
    [Fact] void should_return_the_requested_organization() => Assert.Equal((TenantName)"Acme", _result.OrganizationName);
    [Fact] void should_not_read_publication_progress() => _facts.DidNotReceive().Resolve(_id, Arg.Any<OrganizationSetupProgress?>());
}
#endif
