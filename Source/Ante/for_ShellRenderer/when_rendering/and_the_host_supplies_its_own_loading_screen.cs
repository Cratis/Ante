// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.for_ShellRenderer.when_rendering;

public class and_the_host_supplies_its_own_loading_screen : given_a_built_shell
{
    string _result = null!;

    void Because() => _result = ShellRenderer.Render(Shell, new AnteOptions { SplashHtml = "<p class=\"mine\">One moment</p>", LogoUrl = "/logo.svg" });

    [Fact] void should_use_the_hosts_markup() => _result.ShouldContain("<div id=\"root\"><p class=\"mine\">One moment</p></div>");
    [Fact] void should_not_add_the_default_screen() => _result.ShouldNotContain("<div class=\"ante-splash\"");
}
#endif
