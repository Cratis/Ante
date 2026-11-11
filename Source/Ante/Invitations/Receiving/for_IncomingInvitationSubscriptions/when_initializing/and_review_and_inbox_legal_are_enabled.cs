// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Legal;
using Ante.Contracts.Organization;

namespace Ante.Invitations.Receiving.for_IncomingInvitationSubscriptions.when_initializing;

public class and_review_and_inbox_legal_are_enabled : given.a_lobby_with
{
    void Establish()
    {
        _options.HostStores = ["Operator", "OtherHost"];
        _options.Registration.Review = true;
        _options.Legal.Source = "Inbox";
        _options.Legal.PublisherStore = "Operator";
    }

    Task Because() => Initialize();

    [Fact]
    void should_subscribe_to_legal_publications_only_from_the_publisher()
    {
        Assert.Contains(typeof(LegalDocumentSetPublished).GetEventType().Id, _subscriptions[0].EventTypes);
        Assert.DoesNotContain(typeof(LegalDocumentSetPublished).GetEventType().Id, _subscriptions[1].EventTypes);
    }

    [Fact]
    void should_subscribe_to_review_decisions_from_each_host() =>
        Assert.All(_subscriptions, subscription =>
        {
            Assert.Contains(typeof(OrganizationRegistrationApproved).GetEventType().Id, subscription.EventTypes);
            Assert.Contains(typeof(OrganizationRegistrationDeclineRequested).GetEventType().Id, subscription.EventTypes);
        });
}
#endif
