// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;
using Ante.Organization.Registration.Review.for_RegistrationReviewReceiver.given;

namespace Ante.Organization.Registration.Review.for_RegistrationReviewReceiver.when_declining;

public class and_the_reviewer_gave_no_message : a_review_decision
{
    void Establish() => History = [_held];

    Task Because() => Receiver.Decline(new("   "), Context);

    [Fact] void should_not_decline_it() => ShouldNotHaveAppended();
}
#endif
