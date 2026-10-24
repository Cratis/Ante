// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.FileProviders;

namespace Ante.for_SpaFallback;

public class when_requesting_an_invitation_link : Specification
{
    const string ShellBody = "<html>spa-shell</html>";
    const string DottedToken = "eyJhbGciOiJSUzI1NiJ9.eyJqdGkiOiJhYmMifQ.c2lnbmF0dXJl";

    string _webRoot = null!;
    WebApplication _app = null!;
    HttpClient _client = null!;

    async Task Establish()
    {
        _webRoot = Directory.CreateTempSubdirectory("ante-spa-fallback").FullName;
        await File.WriteAllTextAsync(Path.Combine(_webRoot, SpaFallback.ShellFile), ShellBody);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { WebRootPath = _webRoot });
        builder.WebHost.UseTestServer();
        _app = builder.Build();
        _app.Environment.WebRootFileProvider = new PhysicalFileProvider(_webRoot);
        _app.UseStaticFiles();
        ApiRouteGuard.MapReservedPrefixGuards(_app);
        SpaFallback.Map(_app);

        await _app.StartAsync();
        _client = _app.GetTestServer().CreateClient();
    }

    [Fact]
    async Task should_serve_the_shell_for_a_jwt_shaped_invitation_path()
    {
        var response = await _client.GetAsync(new Uri($"/invite/{DottedToken}", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ShellBody, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    async Task should_still_serve_the_shell_for_an_ordinary_route()
    {
        var response = await _client.GetAsync(new Uri("/register", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    async Task should_not_serve_the_shell_for_a_missing_file_outside_the_invitation_path()
    {
        var response = await _client.GetAsync(new Uri("/assets/missing.js", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    async Task should_not_serve_the_shell_for_a_missing_file_under_the_invitation_path()
    {
        var response = await _client.GetAsync(new Uri("/invite/missing.js", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    async Task should_not_serve_the_shell_for_a_missing_three_part_file_under_the_invitation_path()
    {
        var response = await _client.GetAsync(new Uri("/invite/app.min.js", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("OPTIONS", "/.cratis/me")]
    [InlineData("POST", "/register")]
    [InlineData("PUT", "/register")]
    [InlineData("DELETE", "/register")]
    [InlineData("POST", "/invite/eyJhbGciOiJSUzI1NiJ9.eyJqdGkiOiJhYmMifQ.c2lnbmF0dXJl")]
    async Task should_not_serve_the_shell_for_a_method_other_than_get_or_head(string method, string path)
    {
        var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod(method), new Uri(path, UriKind.Relative)), TestContext.Current.CancellationToken);
        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
    }

    [Fact]
    async Task should_serve_the_shell_for_head()
    {
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Head, new Uri("/register", UriKind.Relative)), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    async Task Destroy()
    {
        await _app.DisposeAsync();
        Directory.Delete(_webRoot, recursive: true);
    }
}
#endif
