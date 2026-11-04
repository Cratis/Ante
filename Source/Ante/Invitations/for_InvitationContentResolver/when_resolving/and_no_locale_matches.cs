// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Globalization;

namespace Ante.Invitations.for_InvitationContentResolver.when_resolving;

public class and_no_locale_matches : Specification
{
    Configuration.InvitationPageContent _result;

    void Because()
    {
        var options = new InvitationSetupOptions
        {
            Content = new Dictionary<string, InvitationContent> { ["fr"] = new() { Title = "French" } },
        };
        _result = InvitationContentResolver.For(options, CultureInfo.GetCultureInfo("nb-NO"));
    }

    [Fact] void should_leave_all_fields_to_ante_defaults() => _result.ShouldEqual(new Configuration.InvitationPageContent("", "", "", ""));
}
#endif
