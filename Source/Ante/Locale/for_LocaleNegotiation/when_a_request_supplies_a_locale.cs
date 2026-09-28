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
    public async Task should_use_the_resolved_cookie_for_validator_and_constraint_messages()
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
        request.Request.Headers.AcceptLanguage = "en-US";
        await builder.Build()(request);

        Assert.Equal("nb-NO", uiCulture);
        Assert.Equal("Organisasjonsnavn er påkrevd.", validatorMessage);
        Assert.Equal("Organisasjonsnavn er påkrevd.", actualValidatorMessage);
        Assert.Contains("En organisasjon med dette navnet finnes allerede.", constraintMessage, StringComparison.Ordinal);
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
