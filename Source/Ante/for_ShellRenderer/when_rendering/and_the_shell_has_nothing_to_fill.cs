// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.for_ShellRenderer.when_rendering;

public class and_the_shell_has_nothing_to_fill : Specification
{
    string _result = null!;

    void Because() => _result = ShellRenderer.Render("<html>spa-shell</html>", new AnteOptions { PageTitle = "Studio" });

    [Fact] void should_return_it_as_it_is() => _result.ShouldEqual("<html>spa-shell</html>");
}
#endif
