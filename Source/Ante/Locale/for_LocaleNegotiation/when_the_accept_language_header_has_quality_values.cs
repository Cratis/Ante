// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Locale.for_LocaleNegotiation;

public class when_the_accept_language_header_has_quality_values : Specification
{
    [Fact]
    public async Task should_skip_a_language_marked_not_acceptable() =>
        Assert.Equal("en-US", await CultureFor("nb-NO;q=0, en;q=0.5"));

    [Fact]
    public async Task should_prefer_the_highest_quality_regardless_of_header_order() =>
        Assert.Equal("nb-NO", await CultureFor("en;q=0.4, nb-NO;q=0.9"));

    [Fact]
    public async Task should_keep_header_order_for_equal_quality() =>
        Assert.Equal("en-US", await CultureFor("en, nb-NO"));

    static async Task<string> CultureFor(string acceptLanguage)
    {
        var builder = new ApplicationBuilder(new ServiceCollection().AddLogging().BuildServiceProvider());
        builder.UseRequestLocalization(LocaleNegotiation.CreateOptions(new AnteOptions()));
        var culture = string.Empty;
        builder.Run(_ =>
        {
            culture = CultureInfo.CurrentUICulture.Name;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Headers.AcceptLanguage = acceptLanguage;
        await builder.Build()(context);
        return culture;
    }
}
#endif
