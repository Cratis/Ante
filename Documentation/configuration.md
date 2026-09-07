---
title: Configuration
description: Every Ante configuration key, its default, and its effect — bound from AnteOptions, InvitationTokenConfig, and IdentityProviderOptions.
---

Ante is configured entirely through `appsettings.json` (or environment variables) — there is no admin UI for any of this. Every key below lives under the `Ante` section, except `IdentityProviders` and `Cratis`, which are their own top-level sections.

## Environment variable form

ASP.NET Core's configuration binder maps a nested key path to an environment variable by joining segments with a double underscore. `Ante:EventStore` becomes `Ante__EventStore`, `Ante:Invitations:Token:Expiry` becomes `Ante__Invitations__Token__Expiry`, and so on. This is how every key in the tables below is set in a container.

## Core options (`Ante`, bound to `AnteOptions`)

| Key | Default | Effect |
|---|---|---|
| `Ante:EventStore` | `Ante` | The Chronicle event store this instance runs against. Never hardcoded anywhere in the source — a second Ante instance in the same cluster is just a different value here. |
| `Ante:Namespace` | `Default` | The fixed Chronicle namespace this instance runs against, within `Ante:EventStore`. Applies to every request this instance serves — Ante is single-tenant per deployment, not request-selected multi-tenant (see [Boundaries](./boundaries.md#multi-tenancy-of-ante-itself)). An empty value fails startup — see [Safe routing](#safe-routing-and-startup-validation) below. |
| `Ante:InboxSourceStore` | `Direct` | Declares which host event store Ante's inbox reactor is compiled to cross-subscribe to. This does **not** retarget the subscription — it exists purely so a value that disagrees with the compiled constant fails startup instead of being silently ignored. See [Known limitation](#known-limitation-the-inbox-source-store-is-not-configurable) below. |
| `Ante:HostAppUrl` | _(empty)_ | Base URL of the host application. The wizards redirect here once an invitation is accepted or a registration completes. Supports a `{tenant}` placeholder, substituted with the organization name at redirect time. |
| `Ante:LogoUrl` | _(empty)_ | URL of a custom logo shown in the lobby. Empty renders a plain "Ante" wordmark; a configured URL that fails to load (404, revoked, blocked) falls back to the wordmark too rather than a broken-image icon. Overridable by mounting a file into the container. |
| `Ante:CustomCssUrl` | _(empty)_ | URL of a custom CSS file loaded into the lobby via a `<link rel="stylesheet">` — see [Boundaries: Branding presets](./boundaries.md#branding-presets) for the trust boundary and what "loaded" actually guarantees. Overridable the same way as `Ante:LogoUrl`. |
| `Ante:IdentityBackchannelUrl` | _(empty)_ | Base URL of a host endpoint Ante calls to pre-flight-check whether a signed-in identity is already associated with a user in the organization being joined. Empty skips the check entirely — see [Host Integration](./host-integration.md#the-identity-backchannel). |
| `Ante:HostOutcomeUrl` | _(empty)_ | Base URL of an optional host endpoint Ante calls to look up what happened to a specific onboarding attempt after Ante's own publication (for example, whether downstream provisioning succeeded or failed). Purely informational — empty skips the lookup and every wizard behaves exactly as it does without this setting. See [Host Integration](./host-integration.md#the-host-outcome-backchannel). |

## Invitation token signing (`Ante:Invitations:Token`, bound to `InvitationTokenConfig`)

| Key | Default | Effect |
|---|---|---|
| `Ante:Invitations:Token:PrivateKeyPem` | _(empty)_ | PEM-encoded RSA private key invitation JWTs are signed with (RS256). Required for token issuance to work at all. |
| `Ante:Invitations:Token:PublicKeyPem` | _(empty)_ | PEM-encoded RSA public key, published for hosts or authentication proxies that verify tokens independently. Ante itself only ever signs. |
| `Ante:Invitations:Token:Issuer` | _(empty)_ | `iss` claim on issued tokens. Left empty, no `iss` claim is validated. |
| `Ante:Invitations:Token:Audience` | _(empty)_ | `aud` claim on issued tokens. Left empty, no `aud` claim is validated. |
| `Ante:Invitations:Token:Expiry` | `7.00:00:00` (7 days) | How long an issued invitation token remains valid, as a .NET `TimeSpan`. |

## Identity provider mirroring (`IdentityProviders`, bound to `IdentityProviderOptions`)

| Key | Default | Effect |
|---|---|---|
| `IdentityProviders:Providers` | _(empty list)_ | Mirrors the identity providers the fronting authentication proxy is configured with — each entry is a `Name` and an optional `Issuer`. Lets a sign-in that carries no `iss` claim (an OAuth2-only provider, which issues none) still be attributed to the correct provider by name. |

## Chronicle and MongoDB (`Cratis`)

| Key | Default | Effect |
|---|---|---|
| `Cratis:MongoDB:Server` | `mongodb://localhost:27017` | The MongoDB connection string read models are persisted to. |
| `Cratis:MongoDB:Database` | `Ante` | The MongoDB database name. |
| `Cratis:Arc:CorrelationId:HttpHeader` | `X-Correlation-Id` | The HTTP header Arc reads/writes for request correlation. |

## Example: a named lobby instance

A reference "Direct" instance runs with:

```bash
Ante__EventStore=DirectLobby
Ante__Namespace=Default
```

Nothing else about Ante's code changes for a second instance — a different `Ante:EventStore` and `Ante:Namespace` (and typically a different `Ante:HostAppUrl` and token keypair) is the entire difference between two lobbies serving two different host products. `Ante:EventStore` and `Ante:Namespace` are independent: change either, both, or neither — a single container image serves any combination purely through configuration, no rebuild.

## Safe routing and startup validation

`AnteRoutingValidator` runs before Chronicle is wired up (`Program.cs`) and fails startup immediately — rather than booting into a misconfigured, silently misrouting instance — when:

- `Ante:EventStore` or `Ante:Namespace` is set to an empty or whitespace-only value.
- `Ante:InboxSourceStore` is set to a value other than the compiled `InboxSourceStore.Name` constant (see below) — an option Ante cannot honor is rejected rather than silently ignored.

A deployment that never sets any of the three keeps booting exactly as it always has; only a deliberately-supplied, invalid value trips the guard.

Separately, four of Ante's own reactors (`OrganizationSetupOutbox`, `JoinTenantAcceptanceOutbox`, `OrganizationRegistrationOutbox`, `LegalTermsAcceptanceOutbox`) forward locally-recorded facts to Ante's own outbox and are pinned with Chronicle's `[EventLog]` attribute. Without it, Chronicle's own store-name inference for the `Cratis.Ante.Contracts` events they handle would compare that assembly's compiled `[EventStore("Ante")]` metadata against whatever `Ante:EventStore` is actually set to — a deployment renamed away from the literal `"Ante"` (exactly what the example above does) would silently mismatch and reroute local forwarding onto a nonexistent inbox sequence instead of the event log, and forwarding to the outbox would silently stop. `[EventLog]` makes each of the four immune to the store's name entirely.

## Known limitation: the inbox source store is not configurable

The Chronicle event store Ante's inbox reactor cross-subscribes to for a host's invitation events (`UserInvitedToJoinTenant`, `UserInvitedToCreateTenant`, `InvitationRevoked`) is **not** truly one of the settings above — the actual subscription target is a compile-time constant in `Source/Ante/Invitations/Receiving/InboxSourceStore.cs`, currently `"Direct"`. Chronicle's `[EventStore]` attribute is the only mechanism for pointing an observer at a store other than its own, and C# requires that attribute argument to be a compile-time constant. Pointing an Ante deployment at a different host store means changing that one constant and rebuilding.

`Ante:InboxSourceStore` exists only to catch a mismatch: it defaults to the compiled constant, and `AnteRoutingValidator` throws at startup if a deployment supplies a different value — turning "silently accepted, quietly ignored" into "loudly rejected". It is not a way to retarget the subscription at runtime. See [Host Integration](./host-integration.md#known-limitation-the-inbox-source-store) for the full detail and the tracking issue, [Cratis/Chronicle#3951](https://github.com/Cratis/Chronicle/issues/3951).

A host writing its own reactor against `Cratis.Ante.Contracts` event types (to observe Ante's outbox) faces the mirror image of this limitation: `Cratis.Ante.Contracts`'s own assembly-level `[EventStore("Ante")]` attribute lets an unattributed host reactor infer Ante's store name automatically, but that attribute is baked into the published package as the literal `"Ante"` — a host observing an Ante instance whose `Ante:EventStore` was renamed away from that default must add an explicit `[EventStore("<the-configured-name>")]` to its own reactor rather than relying on inference.

## Cutover and rollback

Changing `Ante:EventStore` or `Ante:Namespace` on an **already-running** deployment moves it to an empty store/namespace with no history — Chronicle does not migrate events between stores or namespaces. Do this only as a deliberate cutover, not a routine config edit:

1. Stand up the new store/namespace value in a non-production deployment first and let every observer (the read-model projections behind the lobby's own queries) catch up from empty — this is exactly what a fresh Ante instance already does, so there is nothing store/namespace-specific to rehearse beyond confirming the four outbox-forwarding reactors and the inbox reactor register cleanly under the new value.
2. Deploy the consumer side first: any host reactor observing Ante's outbox must already be pointed at (or already tolerate) the new store name before Ante itself cuts over, so no fact forwarded under the new configuration is silently dropped by a host still watching the old one.
3. Cut Ante's own configuration over. Because `Ante:EventStore`/`Ante:Namespace` fully determine where new events land, everything appended from this point is under the new value; nothing under the old store/namespace is touched, deleted, or migrated.
4. **Rollback** is reverting the configuration value — the old store/namespace was never modified, so its data and Chronicle observer checkpoints are exactly as they were. Anything appended under the new value during the cutover window is not carried back automatically; treat a rollback as abandoning that window's facts unless they are manually replayed into the old store.

There is no dual-write, no automatic backfill, and no tooling to replay one store's history into another today — a deployment that needs its accumulated invitation history to follow a store/namespace rename needs a bespoke migration, which is out of scope for Ante itself.

## Guarded routes, health, and diagnostics

None of the settings above configure this — it is fixed behavior, not yet exposed as configuration:

- `/openapi/...` is mapped only when `ASPNETCORE_ENVIRONMENT=Development`. There is no setting to opt a non-development deployment into publishing it.
- `/healthz/ready`'s dependency timeout (`AnteHealthChecks.DependencyTimeout`, 3 seconds) is a compiled constant, not a configuration key.
- The identity backchannel's outage warning never logs the organization name or any other onboarding-specific value, regardless of `Ante:IdentityBackchannelUrl` — see [Deployment: Private diagnostics](./deployment.md#private-diagnostics).

See [Deployment: Health check](./deployment.md#health-check) and [Deployment: Guarded routes](./deployment.md#guarded-routes) for the full behavior.

## Locale

There is no `Ante:*` locale setting because there is nothing yet to select: the lobby ships one supported UI locale, English (`en`), end to end. `Program.cs` pins the backend to `CultureInfo.InvariantCulture` deliberately, so validation messages are always English too — the frontend and the backend cannot drift into different languages for the same rejection, because neither ever varies. A browser requesting any other language falls back to English deterministically; there is no partial translation and no development-only override to preview one. See [Boundaries: Localization](./boundaries.md#localization) for what adding a second locale actually requires.
