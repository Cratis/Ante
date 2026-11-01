// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;
using Ante.Organization.Registration.Review.for_RegistrationReviewReceiver.given;

namespace Ante.Organization.Registration.Review.for_RegistrationReviewReceiver.when_declining;

public class and_the_registration_was_already_declined : a_review_decision
{
    void Establish() => History = [_held, new OrganizationRegistrationDeclineRecorded("jane@example.com", "Jane", MiddleName.NotSet, "Doe", "No")];

    Task Because() => Receiver.Decline(new("Not a fit for this offer"), Context);

    [Fact] void should_not_decline_it_again() => ShouldNotHaveAppended();
}
#endif
