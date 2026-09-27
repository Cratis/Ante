---
title: Configuration
description: Ante runtime, signing, identity, infrastructure, and frontend-build settings with effective defaults.
---

ASP.NET Core reads `appsettings.json`, then environment-specific settings and environment variables. Use `__` for nested environment keys (`Ante__Invitations__Token__Expiry` for `Ante:Invitations:Token:Expiry`). The code default is the bound object's fallback; the base and Development columns show checked-in overrides. Replace example host URLs before deployment.

## Ante runtime options

| Key | Type | Code default | Base / Development setting | When needed |
| --- | --- | --- | --- | --- |
| `Ante:EventStore` | string | `Ante` | `Ante` / `Ante` | Always selects Ante's **own** Chronicle store; nonempty at startup. Not required to override for issuance. |
| `Ante:Namespace` | string | `Default` | unset / unset | Fixed for this instance; nonempty at startup. No automatic history migration on change. |
| `Ante:InboxSourceStore` | string | `Direct` | unset / unset | Validation only. Must equal compiled `InboxSourceStore.Name`; cannot retarget the incoming observer. |
| `Ante:HostAppUrl` | string | empty | `https://{tenant}.example.com/` / `http://{tenant}.localhost:8090/` | Set a **real reachable host URL** before onboarding. `{tenant}` is substituted for organization-creation flows only; **join redirects use the literal configured URL**, so the base `{tenant}.example.com` template breaks join handoff. Use a concrete join destination or plan separate routing. The base value is not a usable production destination. |
| `Ante:LogoUrl` | string | empty | empty / empty | Optional image URL; failed image loads fall back to wordmark. |
| `Ante:CustomCssUrl` | string | empty | empty / empty | Optional CSS URL; same-origin HTTP(S) or cross-origin HTTPS only. See [Customization](./customization.md). |
| `Ante:IdentityBackchannelUrl` | string | empty | empty / unset | Optional host base URL for join-invitation `/in-use` GET. |
| `Ante:HostOutcomeUrl` | string | empty | unset / unset | Optional host base URL for invited-flow `/outcome` GET. |

## Signing options (`Ante:Invitations:Token`)

| Suffix | Type | Code default | Base / Development setting | When needed |
| --- | --- | --- | --- | --- |
| `PrivateKeyPem` | string (PEM) | empty | empty / committed throwaway RSA key | Required to issue invitations and accept exchanges, but empty is allowed at startup for existing deployments: startup warns and all exchanges are rejected, even if `PublicKeyPem` is set. A configured key must parse as an RSA private key. Never reuse the Development key elsewhere. Making it mandatory at startup is tracked in [Ante #69](https://github.com/Cratis/Ante/issues/69). |
| `PublicKeyPem` | string (PEM) | empty | empty / committed throwaway public key | **Additional** trusted exchange-verification key, for overlap during rotation. The public key derived from `PrivateKeyPem` is always trusted; this value does not change which key signs tokens. Distribute the current signing public key separately to external verifiers. |
| `Issuer` | string | empty | empty / empty | Recommended per deployment; written as `iss` at issuance and checked at exchange when set. Empty skips the issuer check and logs a startup warning outside Development. |
| `Audience` | string | empty | empty / empty | Recommended per deployment; written as `aud` at issuance and checked at exchange when set. Empty skips the audience check and logs a startup warning outside Development. |
| `Expiry` | `TimeSpan` | 7 days | `7.00:00:00` / `7.00:00:00` | Issued token lifetime; exchange requires `exp` and validates it with up to 30 seconds of clock skew, but the accepted session expires at the signed `exp`. |

**Upgrade warning:** `PublicKeyPem` was previously ignored by Ante at exchange and is now trusted to verify invitation signatures. Remove any non-Ante key from this setting before upgrading. Supply a second key only when tokens signed with that key should be accepted (for example the outgoing Ante signing key during a rotation). An invalid configured public key prevents startup. Without a private key, an additional public key cannot enable exchange.

Setting `Issuer` or `Audience` on an existing deployment makes links minted before the switch without those claims fail exchange. Tokens live seven days by default; plan the transition around outstanding links. Empty values remain permitted (with production warnings) for this release; requiring them is tracked in [Ante #69](https://github.com/Cratis/Ante/issues/69) for the next major release.

## Identity and infrastructure

| Key | Type | Code default | Base / Development setting | When needed |
| --- | --- | --- | --- | --- |
| `IdentityProviders:Providers` | list of `{Name: string, Issuer: string}` | empty list (fields empty) | unset / unset | Mirror proxy providers for correct attribution, especially issuerless OAuth. Bind indexed entries such as `IdentityProviders__Providers__0__Name` and `IdentityProviders__Providers__0__Issuer`. No match may preserve an unknown reported name; ambiguous absence can resolve to empty. |
| `Cratis:Chronicle:ConnectionString` | Chronicle connection string | client default not set by Ante | unset / unset | Connect to Chronicle: Direct's Ante deployment sets `Cratis__Chronicle__ConnectionString`; verify the target server and client 19.4.7 compatibility. |
| `Cratis:MongoDB:Server` | connection string | framework default not established here | `mongodb://localhost:27017` / inherited | Point at reachable MongoDB for sessions and read models. |
| `Cratis:MongoDB:Database` | string | framework default not established here | `Ante` / inherited | Choose database for instance state; coordinate with Chronicle routing. |
| `Cratis:Arc:CorrelationId:HttpHeader` | string | framework default not established here | `X-Correlation-Id` / inherited | Override only with a matching gateway/host correlation policy. |
| `AllowedHosts` | ASP.NET Core host filter string | framework default not established here | `*` / inherited | Restrict for a deployment's hostnames; this is **not** a substitute for an authenticating proxy. |
| `ASPNETCORE_ENVIRONMENT` | environment name | host-dependent | unset / unset | `Development` exposes OpenAPI and loads Development settings; do not use for production. |
| `ANTE_PRIMEUI_LICENSE` | frontend build environment string | absent | supplied as CI build secret | PrimeReact license read by Vite at frontend build; manage as a build input, not an `Ante:*` runtime option. |

`Cratis` also reads other framework settings (including Chronicle connectivity); this table covers the keys defined by Ante's checked-in settings, not the entire Arc/Chronicle configuration surface. No `Ante:Locale` option exists.

## Routing limits and cutover

Changing `Ante:EventStore` or `Ante:Namespace` selects different local event/read-model routing; it does not move earlier events or guarantee that the destination has no history. Before cutover, check destination history and observer registration, deploy matching host observers, then switch Ante. To roll back, select the previous routing again and reconcile facts written at the destination during the cutover; nothing copies them automatically. The incoming host store stays compiled as `Direct` until `Source/Ante/Invitations/Receiving/InboxSourceStore.cs` is changed and Ante rebuilt; supplying a different `Ante:InboxSourceStore` alone fails startup. The contracts assembly defaults host observers to `Ante`, so a renamed local store requires explicit host observer routing. For a separate lobby, start another instance with, for example, `Ante__EventStore=DirectLobby`, `Ante__Namespace=Default`, its own MongoDB state and a compatible host observer. That still subscribes to host store `Direct`. See [Architecture](./architecture.md).
