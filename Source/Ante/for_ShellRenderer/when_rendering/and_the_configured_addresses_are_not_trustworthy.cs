// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.for_ShellRenderer.when_rendering;

public class and_the_configured_addresses_are_not_trustworthy : given_a_built_shell
{
    string _result = null!;

    void Because() => _result = ShellRenderer.Render(Shell, new AnteOptions
    {
        LogoUrl = "javascript:alert(1)",
        CustomCssUrl = "http://other.example/lobby.css",
    });

    [Fact] void should_not_link_the_stylesheet() => _result.ShouldNotContain("rel=\"stylesheet\"");
    [Fact] void should_not_use_the_logo_address() => _result.ShouldNotContain("javascript:");
    [Fact] void should_fall_back_to_the_wordmark() => _result.ShouldContain("class=\"ante-splash__wordmark\"");
    [Fact] void should_not_accept_a_protocol_relative_address() => ShellRenderer.TrustedStylesheet("//other.example/x.css").ShouldBeNull();
    [Fact] void should_accept_an_https_address() => ShellRenderer.TrustedStylesheet("https://cdn.example/x.css").ShouldEqual("https://cdn.example/x.css");
}
#endif
