// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Organization.Registration.for_SafeLinks;

public class when_checking_links : Specification
{
    [Theory]
    [InlineData("", true)]
    [InlineData("https://cratis.studio/pricing", true)]
    [InlineData("/pricing", true)]
    [InlineData("http://cratis.studio/pricing", false)]
    [InlineData("//evil.example/x", false)]
    [InlineData("/\\evil.example", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("pricing", false)]
    void should_only_allow_https_or_same_origin_paths(string url, bool allowed) => Assert.Equal(allowed, SafeLinks.IsAllowed(url));
}
#endif
