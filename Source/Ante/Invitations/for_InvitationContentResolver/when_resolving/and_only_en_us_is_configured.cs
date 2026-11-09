// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Globalization;

namespace Ante.Invitations.for_InvitationContentResolver.when_resolving;

public class and_only_en_us_is_configured : Specification
{
    Configuration.InvitationPageContent _result;

    void Because()
    {
        var options = new InvitationSetupOptions
        {
            Content = new Dictionary<string, InvitationContent>
            {
                ["EN-us"] = new() { Title = "US English", Intro = "Introduction", CompletionMessage = "Done", SupportUrl = "https://example.com/support" },
            },
        };
        _result = InvitationContentResolver.For(options, CultureInfo.GetCultureInfo("nb-NO"));
    }

    [Fact] void should_fall_back_to_us_english_ignoring_case() => _result.Title.ShouldEqual("US English");
    [Fact] void should_keep_the_introduction() => _result.Intro.ShouldEqual("Introduction");
    [Fact] void should_keep_the_completion_message() => _result.CompletionMessage.ShouldEqual("Done");
    [Fact] void should_keep_the_support_link() => _result.SupportUrl.ShouldEqual("https://example.com/support");
}
#endif
