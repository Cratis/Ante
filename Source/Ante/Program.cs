// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Ante;
using Ante.IdentityProviders;
using Ante.Invitations.Accepting;
using Ante.Invitations.HostOutcome;
using Ante.Invitations.Issuing;
using Ante.Invitations.OrganizationSetup;
using Ante.Invitations.UserSetup;
using Ante.Legal;
using Cratis.Arc;
using Cratis.Arc.MongoDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;

// Force invariant culture for the backend.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// Non-negotiable: routing comes from configuration, never a literal in this file. A second Ante instance
// in the same cluster (e.g. the "DirectLobby" reference instance for the Direct host) runs against its own
// store and/or namespace purely by setting Ante:EventStore / Ante:Namespace (Ante__EventStore /
// Ante__Namespace as environment variables) differently - no code change, no rebuild.
// AnteRoutingValidator turns a genuine misconfiguration (an empty store/namespace, or an InboxSourceStore
// that disagrees with the compiled constant) into a loud startup failure instead of a silent misroute -
// see Documentation/configuration.md.
var anteOptions = builder.Configuration.GetSection("Ante").Get<AnteOptions>() ?? new AnteOptions();
AnteRoutingValidator.Validate(anteOptions);

builder.AddCratis(
    options =>
    {
        options.GeneratedApis.RoutePrefix = "api";
        options.GeneratedApis.IncludeCommandNameInRoute = false;
        options.GeneratedApis.SegmentsToSkipForRoute = 1;
    },
    configureArcBuilder: arcBuilder =>
        arcBuilder.WithMongoDB(configureMongoDB: mongoDBBuilder => mongoDBBuilder.WithCamelCaseNamingPolicy(pluralizeReadModels: true)),
    configureChronicleOptions: options =>
    {
        options.EventStore = anteOptions.EventStore;
        options.ProgramIdentifier = "Cratis Ante";
    },
    configureChronicleBuilder: chronicleBuilder => chronicleBuilder
        .WithCamelCaseNamingPolicy()
        .WithNamespaceResolver(new FixedNamespaceResolver(anteOptions.Namespace)));

builder.Services.AddControllers();
builder.Services.AddMvc();
builder.Services.AddOpenApi();
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(o => o.SuppressModelStateInvalidFilter = true);

builder.Services.Configure<AnteOptions>(builder.Configuration.GetSection("Ante"));
builder.Services.Configure<InvitationTokenConfig>(builder.Configuration.GetSection("Ante:Invitations:Token"));
builder.Services.Configure<IdentityProviderOptions>(builder.Configuration.GetSection(IdentityProviderOptions.ConfigurationSection));

builder.Services.AddSingleton<IIdentityProviderResolver, IdentityProviderResolver>();
builder.Services.AddSingleton<IInvitationTokenIssuer, InvitationTokenIssuer>();
builder.Services.AddScoped<ISignedInIdentity, SignedInIdentity>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<IIdentityBackchannel, IdentityBackchannel>();
builder.Services.AddHttpClient<IHostOutcomeBackchannel, HostOutcomeBackchannel>();
builder.Services.AddIdentityProvider<InvitationIdentityProvider>();
builder.Services.AddSingleton<UserSetupStatusSubscriptions>();
builder.Services.AddSingleton<OrganizationSetupStatusSubscriptions>();

// A host that wants Ante's wizards to collect terms-and-conditions acceptance registers its own
// ILegalDocumentSource - before or after this call, either order works because the last registration
// wins when Arc resolves the single implementation. Without one, this default reports nothing to
// present and every wizard skips the legal step entirely.
builder.Services.TryAddSingleton<ILegalDocumentSource, NoLegalDocumentSource>();

builder.Services.AddAuthorization();

// Bounded, dependency-aware readiness, separate from the unconditional /healthz liveness endpoint
// mapped below - see AnteHealthChecks and Documentation/deployment.md.
builder.Services.AddAnteHealthChecks();

var app = builder.Build();

// Installed before the pipeline (and therefore any traffic) is wired up, so the exchange endpoint and
// InvitationIdentityProvider never run against a collection that is missing the indexes their
// retry-safety and expiry guarantees rely on.
await AcceptedInvitationIndexes.EnsureCreated(app.Services.GetRequiredService<IMongoCollection<AcceptedInvitation>>());

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseWebSockets();
app.UseMiddleware<InviteExchangeBypassMiddleware>();
app.MapControllers();
app.MapOpenApiInDevelopment();
app.UseCratisArc();
app.UseCratisChronicle();
app.MapIdentityProvider();

app.MapAnteHealthChecks();

// Reserved, API-shaped prefixes get a genuine 404 for anything not already matched by a real endpoint
// above, instead of falling through to the SPA shell below - see ApiRouteGuard.
ApiRouteGuard.MapReservedPrefixGuards(app);

app.MapFallbackToFile("index.html");

await app.RunAsync();
