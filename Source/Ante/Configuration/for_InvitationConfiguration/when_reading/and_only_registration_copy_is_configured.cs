// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Configuration.for_InvitationConfiguration.when_reading;

public class and_only_registration_copy_is_configured : Specification
{
    InvitationConfiguration _result;

    void Because()
    {
        var options = new AnteOptions();
        options.Registration.Content["en"] = new() { Title = "Self-registration", Intro = "Registration introduction", CompletionMessage = "Registration completion" };
        _result = InvitationConfiguration.GetInvitationContent(Options.Create(options));
    }

    [Fact] void should_not_change_user_invitation_defaults() => _result.UserSetup.ShouldEqual(new InvitationPageContent("", "", "", ""));
    [Fact] void should_not_change_organization_invitation_defaults() => _result.OrganizationSetup.ShouldEqual(new InvitationPageContent("", "", "", ""));
}
#endif
