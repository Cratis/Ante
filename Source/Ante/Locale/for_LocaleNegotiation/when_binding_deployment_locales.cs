// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Ante.Locale.for_LocaleNegotiation;

public class when_binding_deployment_locales : Specification
{
    [Fact]
    public void should_apply_both_shipped_locales_when_no_list_is_configured()
    {
        var settings = Bind([]);
        var options = LocaleNegotiation.CreateOptions(settings);
        using var publicOptions = JsonDocument.Parse(JsonSerializer.Serialize(LocaleNegotiation.PublicOptions(settings)));

        Assert.Null(settings.SupportedLocales);
        Assert.Equal(["en-US", "nb-NO"], options.SupportedUICultures!.Select(culture => culture.Name));
        Assert.Equal(["en", "nb-NO"], publicOptions.RootElement.GetProperty("supportedLocales").EnumerateArray().Select(locale => locale.GetString()));
    }

    [Fact]
    public void should_not_append_bokmal_to_an_english_only_deployment()
    {
        var settings = Bind(new Dictionary<string, string?> { ["Ante:SupportedLocales:0"] = "en" });
        var options = LocaleNegotiation.CreateOptions(settings);
        using var publicOptions = JsonDocument.Parse(JsonSerializer.Serialize(LocaleNegotiation.PublicOptions(settings)));

        Assert.Equal(["en"], settings.SupportedLocales);
        Assert.Equal(["en-US"], options.SupportedUICultures!.Select(culture => culture.Name));
        Assert.Equal(["en"], publicOptions.RootElement.GetProperty("supportedLocales").EnumerateArray().Select(locale => locale.GetString()));
    }

    static AnteOptions Bind(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build().GetSection("Ante").Get<AnteOptions>() ?? new AnteOptions();
}
#endif
