// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;
using Ante.Organization.Registration.Review.for_RegistrationReviewReceiver.given;

namespace Ante.Organization.Registration.Review.for_RegistrationReviewReceiver.when_approving;

public class and_the_registration_was_already_completed : a_review_decision
{
    void Establish() => History = [_held, _held.ToRecorded()];

    Task Because() => Receiver.Approve(Context);

    [Fact] void should_not_complete_it_twice() => ShouldNotHaveAppended();
}
#endif
