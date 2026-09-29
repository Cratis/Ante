// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Ante;
using Ante.IdentityProviders;
using Ante.Invitations;
using Ante.Invitations.Accepting;
using Ante.Invitations.HostOutcome;
using Ante.Invitations.Issuing;
using Ante.Invitations.OrganizationSetup;
using Ante.Invitations.Receiving;
using Ante.Invitations.UserSetup;
using Ante.Legal;
using Ante.Legal.Receiving;
using Ante.Locale;
using Ante.Organization.Registration;
using Cratis.Arc;
using Cratis.Arc.MongoDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

InvitationMongoSerialization.EnsureConfigured();

var builder = WebApplication.CreateBuilder(args);

// Non-negotiable: routing comes from configuration, never a literal in this file. A second Ante instance
// in the same cluster (e.g. the "DirectLobby" reference instance for the Direct host) runs against its own
// store and/or namespace purely by setting Ante:EventStore / Ante:Namespace (Ante__EventStore /
// Ante__Namespace as environment variables) differently - no code change, no rebuild.
// Validate and freeze the trusted host sources at startup; no routing hot reload.
var anteConfiguration = builder.Configuration.GetSection("Ante");
var anteOptions = anteConfiguration.Get<AnteOptions>() ?? new AnteOptions();
AnteRoutingValidator.Validate(anteOptions, anteConfiguration);
LegalOptions.Validate(anteOptions);
RegistrationOptionsValidator.Validate(anteOptions);
var localizationOptions = LocaleNegotiation.CreateOptions(anteOptions);
var invitationTokenOptions = builder.Configuration.GetSection("Ante:Invitations:Token").Get<InvitationTokenConfig>() ?? new InvitationTokenConfig();
InvitationTokenConfigurationValidator.Validate(invitationTokenOptions);
InvitationTokenIsolation.ApplyDefaults(invitationTokenOptions, anteOptions);
var invitationExchangeOptions = builder.Configuration.GetSection("Ante:Invitations:Exchange").Get<InvitationExchangeConfig>() ?? new InvitationExchangeConfig();
InvitationExchangeConfigurationValidator.Validate(invitationExchangeOptions, invitationTokenOptions);

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

builder.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(anteOptions));
builder.Services.TryAddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IncomingInvitationSubscriptions>();
builder.Services.AddHostedService<IncomingInvitationRegistration>();
builder.Services.Configure<InvitationTokenConfig>(builder.Configuration.GetSection("Ante:Invitations:Token"));
builder.Services.PostConfigure<InvitationTokenConfig>(config => InvitationTokenIsolation.ApplyDefaults(config, anteOptions));
builder.Services.Configure<InvitationExchangeConfig>(builder.Configuration.GetSection("Ante:Invitations:Exchange"));
builder.Services.Configure<IdentityProviderOptions>(builder.Configuration.GetSection(IdentityProviderOptions.ConfigurationSection));

builder.Services.AddSingleton<IIdentityProviderResolver, IdentityProviderResolver>();
builder.Services.AddSingleton<IInvitationTokenIssuer, InvitationTokenIssuer>();
builder.Services.AddSingleton<IInvitationTokenValidator, InvitationTokenValidator>();
builder.Services.AddInvitationTokenUpgradeWindow(invitationTokenOptions.Expiry);
builder.Services.AddSingleton<InvitationAttestationVerifier>();
builder.Services.AddScoped<AttestedInvitationStaging>();
builder.Services.AddScoped<AttestedInvitationCompletion>();
builder.Services.AddScoped<IAttestedInvitationSessions, AttestedInvitationSessions>();
builder.Services.AddScoped<IInvitationAcceptanceFence, InvitationAcceptanceFence>();
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
if (anteOptions.Legal.Source == "Inbox")
{
    builder.Services.AddScoped<ILegalDocumentSource, InboxLegalDocumentSource>();
}
else
{
    builder.Services.TryAddSingleton<ILegalDocumentSource, NoLegalDocumentSource>();
}

builder.Services.AddAuthorization();
builder.Services.AddRegistrationRateLimiting(anteOptions);

// Bounded, dependency-aware readiness, separate from the unconditional /healthz liveness endpoint
// mapped below - see AnteHealthChecks and Documentation/deployment.md.
builder.Services.AddAnteHealthChecks()
    .AddCheck<IncomingRoutingHealthCheck>("host-routing", tags: [AnteHealthChecks.ReadyTag], timeout: AnteHealthChecks.DependencyTimeout);

var app = builder.Build();
InvitationTokenConfigurationValidator.Report(
    invitationTokenOptions,
    app.Services.GetRequiredService<ILogger<InvitationTokenConfigurationValidator>>());

// Record, before serving, when this deployment first isolated its tokens - that opens the upgrade window.
app.Services.GetRequiredService<IInvitationTokenUpgradeWindow>();
IdentityProviderConfigurationWarnings.WarnForUnattributableSignIns(
    app.Services.GetRequiredService<IOptions<IdentityProviderOptions>>().Value,
    app.Services.GetRequiredService<ILogger<IdentityProviderOptions>>());

// Installed before the pipeline (and therefore any traffic) is wired up, so the exchange endpoint and
// InvitationIdentityProvider never run against a collection that is missing the indexes their
// retry-safety and expiry guarantees rely on.
// IMongoCollection<T> is scoped (its database follows the current tenant), so it is resolved from a
// scope rather than the root provider; Development's scope validation rejects the root resolution.
await using (var startupScope = app.Services.CreateAsyncScope())
{
    await AcceptedInvitationIndexes.EnsureCreated(startupScope.ServiceProvider.GetRequiredService<IMongoCollection<AcceptedInvitation>>());
    if (invitationExchangeOptions.Mode == InvitationExchangeMode.Attested)
    {
        await StagedInvitationTransactionIndexes.EnsureCreated(startupScope.ServiceProvider.GetRequiredService<IMongoCollection<StagedInvitationTransaction>>());
        await AttestedInvitationSessionIndexes.EnsureCreated(startupScope.ServiceProvider.GetRequiredService<IMongoCollection<AttestedInvitationSession>>());
    }
}

app.UseWebSockets();
app.UseRequestLocalization(localizationOptions);

// UI messages follow the request; parsing and numeric/identity semantics do not.
app.Use(async (context, next) =>
{
    CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    await next(context);
});
app.UseRouting();
app.UseRegistrationRateLimiting(anteOptions);
app.Use(LocalizedConstraintResponses.Invoke);
app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

// Branch before the legacy exchange middleware so the attested handlers (which depend on Chronicle)
// are resolved only for attested stage and completion POSTs, never for health or ordinary routes.
if (invitationExchangeOptions.Mode == InvitationExchangeMode.Attested)
{
    app.MapWhen(
        context => HttpMethods.IsPost(context.Request.Method) &&
            (context.Request.Path.Equals("/_invite/stage", StringComparison.OrdinalIgnoreCase) ||
             context.Request.Path.Equals("/_invite/exchange", StringComparison.OrdinalIgnoreCase)),
        branch => branch.UseMiddleware<AttestedInviteExchangeMiddleware>());
}

app.UseMiddleware<InviteExchangeBypassMiddleware>();
app.MapControllers();
app.MapOpenApiInDevelopment();
app.UseCratisArc();
app.UseCratisChronicle();

// The hosted registration retries off the startup path; waiting here would block readiness and startup.
app.MapIdentityProvider();

app.MapAnteHealthChecks();
app.MapGet("/api/locale-config", () => Results.Json(LocaleNegotiation.PublicOptions(anteOptions)))
    .AllowAnonymous();

// Reserved, API-shaped prefixes get a genuine 404 for anything not already matched by a real endpoint
// above, instead of falling through to the SPA shell below - see ApiRouteGuard.
ApiRouteGuard.MapReservedPrefixGuards(app);

SpaFallback.Map(app);

await app.RunAsync();
