// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.Routes.given;

namespace Ante.Integration.Routes.when_visiting_the_public_shell;

/// <summary>
/// The single-page application shell, its assets and the invitation and registration pages are public: an
/// invitee or a visitor reaches them before they are signed in, and being signed in changes nothing. Shared by every
/// cell of the route matrix, which only choose the environment and how Ante is hosted.
/// </summary>
public abstract class visiting_the_public_shell : a_routed_ante
{
    static readonly string[] _clientRoutes = ["/", "/index.html", "/register", "/register/", "/register/details", "/invite", "/invite/", "/some/client/route"];
    static readonly string[] _invitationLink = [$"/invite/{InvitationLinkToken}"];
    static readonly string[] _assets = ["/assets/app.js"];
    static readonly string[] _missingFiles = ["/assets/missing.js", "/invite/app.min.js"];
    static readonly string[] _outsideTheWebRoot = ["/appsettings.json", "/assets/../appsettings.json", "/Program.cs"];

    Visits _visits;

    async Task Because() => _visits = await Visit([.. _clientRoutes, .. _invitationLink, .. _assets, .. _missingFiles, .. _outsideTheWebRoot]);

    [Fact] public void should_serve_the_shell_for_client_routes_to_everyone() => _visits.Failing(_clientRoutes, reply => reply.IsOk && reply.IsShell && reply.ContentType!.StartsWith("text/html")).ShouldBeEmpty();
    [Fact] public void should_serve_the_shell_for_an_invitation_link_to_everyone() => _visits.Failing(_invitationLink, reply => reply.IsOk && reply.IsShell).ShouldBeEmpty();
    [Fact] public void should_serve_assets_from_the_web_root_to_everyone() => _visits.Failing(_assets, reply => reply.IsOk && reply.Body.Contains("globalThis.ante") && !reply.IsShell).ShouldBeEmpty();
    [Fact] public void should_answer_a_missing_file_with_404_instead_of_the_shell() => _visits.Failing(_missingFiles, reply => reply.Status == System.Net.HttpStatusCode.NotFound && !reply.IsShell).ShouldBeEmpty();
    [Fact] public void should_not_serve_files_outside_the_web_root() => _visits.Failing(_outsideTheWebRoot, reply => reply.Status == System.Net.HttpStatusCode.NotFound && !reply.IsShell && !reply.Body.Contains("Ante")).ShouldBeEmpty();
    [Fact] public void should_not_set_a_cookie() => _visits.Failing(reply => reply.SetCookies.Count == 0).ShouldBeEmpty();
}
