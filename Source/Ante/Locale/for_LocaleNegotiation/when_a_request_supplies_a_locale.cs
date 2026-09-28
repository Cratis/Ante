// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Globalization;
using Ante.Organization;
using Ante.Resources;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Locale.for_LocaleNegotiation;

public class when_a_request_supplies_a_locale : Specification
{
    [Fact]
    public async Task should_use_the_resolved_cookie_for_event_stream_messages()
    {
        var options = LocaleNegotiation.CreateOptions(new AnteOptions());
        var builder = new ApplicationBuilder(new ServiceCollection().AddLogging().BuildServiceProvider());
        builder.UseRequestLocalization(options);
        string? uiCulture = null;
        string? validatorMessage = null;
        string? constraintMessage = null;
        string? actualValidatorMessage = null;
        builder.Run(async context =>
        {
            uiCulture = CultureInfo.CurrentUICulture.Name;
            validatorMessage = Messages.Get("OrganizationNameRequired");
            var validator = new InlineValidator<string>();
            validator.RuleFor(value => value).MustBeAValidOrganizationName();
            actualValidatorMessage = (await validator.ValidateAsync(string.Empty)).Errors[0].ErrorMessage;
            constraintMessage = LocalizedConstraintResponses.Translate("{\"validationResults\":[{\"reason\":\"constraintViolation\",\"reasonDetail\":\"UniqueOrganizationName\",\"message\":\"An organization with this name already exists.\"}]}");
        });
        var request = new DefaultHttpContext();
        request.Request.Headers.Cookie = "ante-locale=nb-NO";
        request.Request.Headers.Accept = "text/event-stream";
        request.Request.Headers.AcceptLanguage = "en-US";
        await builder.Build()(request);

        Assert.Equal("nb-NO", uiCulture);
        Assert.Equal("Organisasjonsnavn er påkrevd.", validatorMessage);
        Assert.Equal("Organisasjonsnavn er påkrevd.", actualValidatorMessage);
        Assert.Contains("En organisasjon med dette navnet finnes allerede.", constraintMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task should_prefer_each_tab_header_over_the_shared_cookie_for_arc_http()
    {
        var builder = new ApplicationBuilder(new ServiceCollection().AddLogging().BuildServiceProvider());
        builder.UseRequestLocalization(LocaleNegotiation.CreateOptions(new AnteOptions()));
        var cultures = new List<string>();
        builder.Run(_ =>
        {
            cultures.Add(CultureInfo.CurrentUICulture.Name);
            return Task.CompletedTask;
        });
        var pipeline = builder.Build();
        var englishTab = new DefaultHttpContext();
        englishTab.Request.Headers.Cookie = "ante-locale=nb-NO";
        englishTab.Request.Headers.AcceptLanguage = "en-US";
        await pipeline(englishTab);
        var bokmalTab = new DefaultHttpContext();
        bokmalTab.Request.Headers.Cookie = "ante-locale=en";
        bokmalTab.Request.Headers.AcceptLanguage = "nb-NO";
        await pipeline(bokmalTab);

        Assert.Equal(["en-US", "nb-NO"], cultures);
    }

    [Fact]
    public async Task should_use_the_cookie_for_a_websocket_upgrade_in_the_production_pipeline_order()
    {
        var builder = new ApplicationBuilder(new ServiceCollection().AddLogging().BuildServiceProvider());
        builder.UseWebSockets();
        builder.UseRequestLocalization(LocaleNegotiation.CreateOptions(new AnteOptions()));
        string? culture = null;
        builder.Run(_ =>
        {
            culture = CultureInfo.CurrentUICulture.Name;
            return Task.CompletedTask;
        });
        var request = new DefaultHttpContext();
        request.Request.Headers.Cookie = "ante-locale=nb-NO";
        request.Request.Headers.AcceptLanguage = "en-US";
        request.Request.Headers.Connection = "Upgrade";
        request.Request.Headers.Upgrade = "websocket";
        await builder.Build()(request);

        Assert.Equal("nb-NO", culture);
    }

    [Fact]
    public void should_reject_a_default_outside_the_deployment_allowlist() =>
        Assert.Throws<InvalidOperationException>(() => LocaleNegotiation.CreateOptions(new AnteOptions
        {
            DefaultLocale = "en",
            SupportedLocales = ["nb-NO"]
        }));

    [Fact]
    public async Task should_accept_the_no_alias_from_accept_language()
    {
        var builder = new ApplicationBuilder(new ServiceCollection().AddLogging().BuildServiceProvider());
        builder.UseRequestLocalization(LocaleNegotiation.CreateOptions(new AnteOptions()));
        string? uiCulture = null;
        builder.Run(_ =>
        {
            uiCulture = CultureInfo.CurrentUICulture.Name;
            return Task.CompletedTask;
        });
        var request = new DefaultHttpContext();
        request.Request.Headers.AcceptLanguage = "nn-NO, no;q=0.8, en-US;q=0.5";
        await builder.Build()(request);
        Assert.Equal("nb-NO", uiCulture);
    }
}
#endif
