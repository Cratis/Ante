// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Ante.for_SpaFallback;

// The first thing a visitor sees - on the root, on an invitation link and on any other application route - is
// the host's own loading screen, because it is written into the shell on the server.
public class when_the_host_brands_the_loading_screen : Specification
{
    const string Shell = "<html><head><title>Ante</title></head><body><div id=\"root\"></div><script src=\"/assets/app.js\"></script></body></html>";
    const string Token = "eyJhbGciOiJSUzI1NiJ9.eyJqdGkiOiJhYmMifQ.c2lnbmF0dXJl";

    string _webRoot = null!;
    WebApplication _app = null!;
    HttpClient _client = null!;

    async Task Establish()
    {
        _webRoot = Directory.CreateTempSubdirectory("ante-spa-branding").FullName;
        await File.WriteAllTextAsync(Path.Combine(_webRoot, SpaFallback.ShellFile), Shell);
        await File.WriteAllTextAsync(Path.Combine(_webRoot, "other.txt"), "plain");

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { WebRootPath = _webRoot });
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(Options.Create(new AnteOptions { PageTitle = "Studio", LogoUrl = "/logo.svg" }));
        _app = builder.Build();
        _app.Environment.WebRootFileProvider = new PhysicalFileProvider(_webRoot);
        SpaFallback.UseShell(_app);
        _app.UseStaticFiles();
        SpaFallback.Map(_app);

        await _app.StartAsync();
        _client = _app.GetTestServer().CreateClient();
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/index.html")]
    [InlineData("/register")]
    [InlineData($"/invite/{Token}")]
    async Task should_show_the_hosts_screen_from_the_first_byte(string path)
    {
        var response = await _client.GetAsync(new Uri(path, UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<title>Studio</title>", body, StringComparison.Ordinal);
        Assert.Contains("src=\"/logo.svg\"", body, StringComparison.Ordinal);
        Assert.Contains("/assets/app.js", body, StringComparison.Ordinal);
    }

    [Fact]
    async Task should_not_let_a_browser_keep_a_copy_of_the_shell()
    {
        var response = await _client.GetAsync(new Uri("/", UriKind.Relative));
        Assert.Contains("no-cache", response.Headers.CacheControl!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    async Task should_still_serve_other_static_files_untouched()
    {
        Assert.Equal("plain", await _client.GetStringAsync(new Uri("/other.txt", UriKind.Relative)));
    }

    async Task Destroy()
    {
        await _app.DisposeAsync();
        Directory.Delete(_webRoot, recursive: true);
    }
}
#endif
