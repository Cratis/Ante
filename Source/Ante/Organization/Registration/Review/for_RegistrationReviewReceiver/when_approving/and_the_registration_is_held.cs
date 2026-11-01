// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Contracts.Organization;
using Ante.Organization.Registration.Review.for_RegistrationReviewReceiver.given;

namespace Ante.Organization.Registration.Review.for_RegistrationReviewReceiver.when_approving;

public class and_the_registration_is_held : a_review_decision
{
    void Establish() => History = [_held];

    Task Because() => Receiver.Approve(Context);

    [Fact] void should_complete_the_registration_as_it_would_have_been_without_review() => ShouldHaveAppended<OrganizationRegistrationRecorded>(
        recorded => recorded.TenantName == "Acme" && recorded.Subject == "sub-1" && recorded.Email == "jane@example.com");
}
#endif
