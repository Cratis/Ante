// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Invitations;
using Ante.Contracts.Onboarding;
using Ante.Fixture.Host;

if (args is ["healthcheck"])
{
    using var client = new HttpClient();
    try
    {
        using var response = await client.GetAsync("http://localhost:5100/ready");
        return response.IsSuccessStatusCode ? 0 : 1;
    }
    catch (HttpRequestException)
    {
        return 1;
    }
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<Invitations>();
builder.Services.AddHostedService<Provisioning>();
var app = builder.Build();
var invitations = app.Services.GetRequiredService<Invitations>();
await invitations.Connect(app.Configuration);
app.MapGet("/ready", () => Results.Ok());
app.MapPost("/invitations", async (InviteRequest request) => new { invitationId = await invitations.Invite(request) });
app.MapGet("/invitations/{id:guid}", async (Guid id) =>
{
    var received = (await invitations.Received(id)).ToArray();
    var issued = received.Select(entry => entry.Content).OfType<InvitationTokenIssued>().FirstOrDefault();
    return new
    {
        tokenIssued = issued is not null,
        link = issued is null ? null : $"{app.Configuration["Fixture:PublicBaseUrl"]}/invite/{issued.Token}",
        accepted = received.Any(entry => entry.Content is InvitationToJoinTenantAccepted),
        provisioningConfirmed = received.Any(entry => entry.Content is OnboardingProvisioningConfirmed),
        received = received.Select(entry => entry.Content.GetType().Name),
    };
});
app.MapGet("/", () => Results.Content("<!doctype html><title>Host application</title><h1>Host application</h1>", "text/html"));
await app.RunAsync();
return 0;
