// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Globalization;

namespace Ante.Invitations.for_InvitationContentResolver.when_resolving;

public class and_the_selected_locale_is_partial : Specification
{
    Configuration.InvitationPageContent _result;

    void Because()
    {
        var options = new InvitationSetupOptions
        {
            Content = new Dictionary<string, InvitationContent>
            {
                ["nb-NO"] = new() { Intro = "<b>Literal text</b>" },
                ["en"] = new() { Title = "English", CompletionMessage = "English completion", SupportUrl = "https://example.com/" },
            },
        };
        _result = InvitationContentResolver.For(options, CultureInfo.GetCultureInfo("nb-NO"));
    }

    [Fact] void should_preserve_literal_text_for_safe_rendering() => _result.Intro.ShouldEqual("<b>Literal text</b>");
    [Fact] void should_leave_the_missing_title_to_the_localized_default() => _result.Title.ShouldEqual("");
    [Fact] void should_leave_the_missing_completion_to_the_localized_default() => _result.CompletionMessage.ShouldEqual("");
    [Fact] void should_not_inherit_another_locales_support_link() => _result.SupportUrl.ShouldEqual("");
}
#endif
