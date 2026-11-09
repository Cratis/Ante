// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.for_ShellRenderer.when_rendering;

public class and_the_host_brands_the_loading_screen : given_a_built_shell
{
    string _result = null!;

    void Because() => _result = ShellRenderer.Render(Shell, new AnteOptions
    {
        PageTitle = "Studio <beta> & co",
        SplashMessage = "Loading Studio",
        LogoUrl = "/branding/logo.svg",
        CustomCssUrl = "/branding/lobby.css",
    });

    [Fact] void should_use_the_title_encoded() => _result.ShouldContain("<title>Studio &lt;beta&gt; &amp; co</title>");
    [Fact] void should_show_the_logo_instead_of_the_wordmark() => _result.ShouldContain("<img class=\"ante-splash__logo\" src=\"/branding/logo.svg\"");
    [Fact] void should_not_show_the_wordmark() => _result.ShouldNotContain("class=\"ante-splash__wordmark\"");
    [Fact] void should_load_the_hosts_stylesheet() => _result.ShouldContain("<link rel=\"stylesheet\" href=\"/branding/lobby.css\" />");
    [Fact] void should_use_the_hosts_announcement() => _result.ShouldContain("aria-label=\"Loading Studio\"");
    [Fact] void should_put_the_screen_inside_the_root_the_application_replaces() => _result.ShouldContain("<div id=\"root\"><div class=\"ante-splash\"");
}
#endif
