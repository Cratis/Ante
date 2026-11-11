// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Invitations.OrganizationSetup;
using Ante.Invitations.UserSetup;
using Ante.Legal;
using Ante.Organization.Registration;
using Ante.Organization.Registration.Review;
using Cratis.Specifications;
using Xunit;

namespace Ante.Events.for_RestoredGenerations;

public class when_inspecting_the_restored_generation_one_events : Specification
{
    static readonly (Type Current, Type Generation1)[] _events =
    [
        (typeof(OrganizationSetupCompleted), typeof(OrganizationSetupCompletedV1)),
        (typeof(UserSetupCompleted), typeof(UserSetupCompletedV1)),
        (typeof(LegalTermsAcceptanceRecorded), typeof(LegalTermsAcceptanceRecordedV1)),
        (typeof(OrganizationRegistrationRecorded), typeof(OrganizationRegistrationRecordedV1)),
        (typeof(OrganizationRegistrationHeld), typeof(OrganizationRegistrationHeldV1)),
    ];

    [Fact]
    void should_keep_the_identity_properties_out_of_generation_one() =>
        Assert.All(_events, e =>
        {
            Assert.Null(e.Generation1.GetProperty("IdentityProviderKey"));
            Assert.Null(e.Generation1.GetProperty("IdentityIssuer"));
        });

    [Fact]
    void should_carry_the_identity_properties_in_the_current_generation() =>
        Assert.All(_events, e =>
        {
            Assert.NotNull(e.Current.GetProperty("IdentityProviderKey"));
            Assert.NotNull(e.Current.GetProperty("IdentityIssuer"));
        });

    [Fact]
    void should_mark_the_current_type_as_generation_two() =>
        Assert.All(_events, e => Assert.Equal(2u, ((EventTypeAttribute)Attribute.GetCustomAttribute(e.Current, typeof(EventTypeAttribute))!).Generation.Value));

    [Fact]
    void should_have_the_same_event_name_for_both_generations() =>
        Assert.All(_events, e => Assert.Equal(e.Current.Name, e.Generation1.Name[..^2]));
}
