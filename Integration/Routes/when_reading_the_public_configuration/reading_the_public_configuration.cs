// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Ante.Integration.Routes.given;

namespace Ante.Integration.Routes.when_reading_the_public_configuration;

/// <summary>
/// What the lobby needs before anyone signs in - branding, registration, locales, legal documents and where the host
/// lives - is public and reads the same for an anonymous visitor and a signed-in user.
/// </summary>
public abstract class reading_the_public_configuration : a_routed_ante
{
    const string HostUrl = "/api/configuration/host-url";
    static readonly string[] _queries =
    [
        HostUrl,
        "/api/configuration/get-configuration",
        "/api/configuration/registration",
        "/api/legal/current",
    ];

    Visits _visits;
    Visits _localeVisits;

    async Task Because()
    {
        _visits = await Visit(_queries);
        _localeVisits = await Visit(["/api/locale-config"]);
    }

    [Fact] void should_answer_every_query_to_everyone() => _visits.Failing(reply => reply.IsOk && reply.IsSuccess).ShouldBeEmpty();
    [Fact] void should_not_ask_anyone_to_sign_in() => _visits.Failing(reply => reply.Json.GetProperty("isAuthorized").GetBoolean()).ShouldBeEmpty();
    [Fact] void should_publish_the_branding_to_an_anonymous_visitor() => _visits.Anonymous("/api/configuration/get-configuration").Data.GetProperty("logoUrl").GetString().ShouldEqual(string.Empty);
    [Fact] void should_open_registration_to_an_anonymous_visitor() => _visits.Anonymous("/api/configuration/registration").Data.GetProperty("isEnabled").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_that_no_legal_documents_are_configured() => _visits.Anonymous("/api/legal/current").Data.GetProperty("isConfigured").GetBoolean().ShouldBeFalse();
    [Fact] void should_answer_a_signed_in_user_exactly_as_an_anonymous_visitor() => _queries.Where(path => _visits.Anonymous(path).Data.ToString() != _visits.SignedIn(path).Data.ToString()).ShouldBeEmpty();
    [Fact] void should_publish_the_supported_locales_to_everyone() => _localeVisits.Failing(["/api/locale-config"], reply => reply.IsOk && reply.Json.GetProperty("defaultLocale").GetString() == "en" && reply.Json.GetProperty("supportedLocales").GetArrayLength() == 2).ShouldBeEmpty();

    // Development layers appsettings.Development.json over the base settings; any other environment keeps the base template.
    [Fact] void should_publish_the_host_url_of_the_environment() => _visits.Anonymous(HostUrl).Data.GetProperty("hostAppUrl").GetString().ShouldEqual(IsDevelopment ? "http://{tenant}.localhost:8090/" : "https://{tenant}.example.com/");
    [Fact] void should_not_set_a_cookie() => _visits.Failing(reply => reply.SetCookies.Count == 0).ShouldBeEmpty();
}
