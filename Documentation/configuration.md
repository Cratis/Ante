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
| `Ante:HostAppUrl` | _(empty)_ | Base URL of the host application. The wizards redirect here once an invitation is accepted or a registration completes. Supports a `{tenant}` placeholder, substituted with the organization name at redirect time. |
| `Ante:LogoUrl` | _(empty)_ | URL of a custom logo shown in the lobby. Empty renders a plain "Ante" wordmark. Overridable by mounting a file into the container. |
| `Ante:CustomCssUrl` | _(empty)_ | URL of a custom CSS file injected into the lobby. Overridable the same way. |
| `Ante:IdentityBackchannelUrl` | _(empty)_ | Base URL of a host endpoint Ante calls to pre-flight-check whether a signed-in identity is already associated with a user in the organization being joined. Empty skips the check entirely — see [Host Integration](./host-integration.md#the-identity-backchannel). |

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
```

Nothing else about Ante's code changes for a second instance — a different `Ante:EventStore` (and typically a different `Ante:HostAppUrl` and token keypair) is the entire difference between two lobbies serving two different host products.

## Known limitation: the inbox source store is not configurable

The Chronicle event store Ante's inbox reactor cross-subscribes to for a host's invitation events (`UserInvitedToJoinTenant`, `UserInvitedToCreateTenant`, `InvitationRevoked`) is **not** one of the settings above — it is a compile-time constant in `Source/Ante/Invitations/Receiving/InboxSourceStore.cs`, currently `"Direct"`. Chronicle's `[EventStore]` attribute is the only mechanism for pointing an observer at a store other than its own, and C# requires that attribute argument to be a compile-time constant. Pointing an Ante deployment at a different host store means changing that one constant and rebuilding. See [Host Integration](./host-integration.md#known-limitation-the-inbox-source-store) for the full detail and the tracking issue.
