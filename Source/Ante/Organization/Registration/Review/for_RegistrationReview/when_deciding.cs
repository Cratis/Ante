// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Organization.Registration.Review.for_RegistrationReview;

public class when_deciding : Specification
{
    static readonly OrganizationRegistrationHeld _held = new("Acme", "sub-1", "github", "Jane", MiddleName.NotSet, "Doe", "jane@example.com", []);
    static readonly OrganizationRegistrationDeclineRecorded _declined = new("jane@example.com", "Jane", MiddleName.NotSet, "Doe", "No");

    [Fact] void should_hold_a_held_registration() => Assert.Equal(RegistrationReviewState.Held, RegistrationReview.Decide([_held]).State);
    [Fact] void should_name_the_requested_organization() => Assert.Equal((TenantName)"Acme", RegistrationReview.Decide([_held]).OrganizationName);
    [Fact] void should_not_hold_an_approved_registration() => Assert.Equal(RegistrationReviewState.NotHeld, RegistrationReview.Decide([_held, _held.ToRecorded()]).State);
    [Fact] void should_not_hold_a_registration_that_was_never_held() => Assert.Equal(RegistrationReviewState.NotHeld, RegistrationReview.Decide([]).State);
    [Fact] void should_report_a_declined_registration() => Assert.Equal(RegistrationReviewState.Declined, RegistrationReview.Decide([_held, _declined]).State);
}
#endif
