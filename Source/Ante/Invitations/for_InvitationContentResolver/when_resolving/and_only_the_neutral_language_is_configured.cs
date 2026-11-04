// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Globalization;

namespace Ante.Invitations.for_InvitationContentResolver.when_resolving;

public class and_only_the_neutral_language_is_configured : Specification
{
    Configuration.InvitationPageContent _result;

    void Because()
    {
        var options = new InvitationSetupOptions
        {
            Content = new Dictionary<string, InvitationContent>
            {
                ["NB"] = new() { Title = "Neutral" },
                ["en"] = new() { Title = "English" },
            },
        };
        _result = InvitationContentResolver.For(options, CultureInfo.GetCultureInfo("nb-NO"));
    }

    [Fact] void should_prefer_the_neutral_language_ignoring_case() => _result.Title.ShouldEqual("Neutral");
}
#endif
