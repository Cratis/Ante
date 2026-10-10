// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.for_ShellRenderer.when_rendering;

public class and_nothing_is_configured : given_a_built_shell
{
    string _result = null!;

    void Because() => _result = ShellRenderer.Render(Shell, new AnteOptions());

    [Fact] void should_keep_the_default_title() => _result.ShouldContain("<title>Ante</title>");
    [Fact] void should_show_the_wordmark_while_loading() => _result.ShouldContain("class=\"ante-splash__wordmark\"");
    [Fact] void should_show_a_spinner() => _result.ShouldContain("ante-splash__spinner");
    [Fact] void should_announce_that_it_is_loading() => _result.ShouldContain("role=\"status\" aria-label=\"Loading\"");
    [Fact] void should_not_link_a_stylesheet() => _result.ShouldNotContain("rel=\"stylesheet\"");
    [Fact] void should_keep_the_application_script() => _result.ShouldContain("/assets/app.js");
}
#endif
