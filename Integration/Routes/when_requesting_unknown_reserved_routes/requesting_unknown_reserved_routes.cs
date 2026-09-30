// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Ante.Integration.Routes.given;

namespace Ante.Integration.Routes.when_requesting_unknown_reserved_routes;

/// <summary>
/// A path under a reserved, API-shaped prefix that no endpoint claims is a real 404 - never the single-page
/// application shell a caller would read as success - and the generated OpenAPI document exists only in Development.
/// </summary>
public abstract class requesting_unknown_reserved_routes : a_routed_ante
{
    const string OpenApiDocument = "/openapi/v1.json";
    static readonly string[] _unknown =
    [
        "/api", "/api/", "/api/nope", "/Api/nope", "/api/configuration/nope", "/api/invitations/nope/deeper",
        "/_invite", "/_invite/nope", "/healthz/nope", "/openapi",
    ];

    Visits _visits;
    Reply _unknownPost;

    async Task Because()
    {
        _visits = await Visit([.. _unknown, OpenApiDocument]);
        _unknownPost = await Send(HttpMethod.Post, "/api/nope", body: new { });
    }

    [Fact] void should_answer_an_unknown_reserved_path_with_404() => _visits.Failing(_unknown, reply => reply.Status == HttpStatusCode.NotFound).ShouldBeEmpty();
    [Fact] void should_never_answer_an_unknown_reserved_path_with_the_shell() => _visits.Failing(_unknown, reply => !reply.IsShell).ShouldBeEmpty();
    [Fact] void should_answer_an_unknown_command_route_with_404() => _unknownPost.Status.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_publish_the_openapi_document_only_in_development() => _visits.Failing([OpenApiDocument], reply => IsDevelopment ? reply.IsOk && reply.Json.GetProperty("openapi").ValueKind == System.Text.Json.JsonValueKind.String : reply.Status == HttpStatusCode.NotFound && !reply.IsShell).ShouldBeEmpty();
}
