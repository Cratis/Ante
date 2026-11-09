// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Globalization;

namespace Ante.Configuration.for_InvitationConfiguration.when_reading;

public class and_the_journeys_have_independent_copy : Specification
{
    InvitationConfiguration _invitations;
    RegistrationConfiguration _registration;

    void Because()
    {
        var options = new AnteOptions();
        options.Invitations.UserSetup.Content["en"] = new() { Title = "User invitation" };
        options.Invitations.OrganizationSetup.Content["en"] = new() { Title = "Organization invitation" };
        options.Registration.Content["en"] = new() { Title = "Self-registration" };
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
            _invitations = InvitationConfiguration.GetInvitationContent(Options.Create(options));
            _registration = RegistrationConfiguration.Registration(Options.Create(options));
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact] void should_expose_only_the_user_journeys_copy_for_user_setup() => _invitations.UserSetup.Title.ShouldEqual("User invitation");
    [Fact] void should_expose_only_the_organization_journeys_copy_for_organization_setup() => _invitations.OrganizationSetup.Title.ShouldEqual("Organization invitation");
    [Fact] void should_leave_registration_independent() => _registration.Content.Title.ShouldEqual("Self-registration");
}
#endif
