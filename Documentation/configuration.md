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
| `Ante:HostStores` | list of strings | `["Direct"]` | unset / unset | Trusted host outbox stores to receive from. Each has a separate `inbox-{store}` reactor and subscription. Cannot be empty, contain blank/duplicate entries, or include `Ante:EventStore` (ordinal comparisons). Restart after changing. |
| `Ante:InboxSourceStore` | string | unset | unset / unset | Deprecated single-store alias. Used only if `HostStores` is absent; both settings may coexist only if the list has exactly the same one source. An explicitly empty value fails startup. |
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

**Next-major migration ([Ante #69](https://github.com/Cratis/Ante/issues/69)):** `Issuer`, `Audience` and a usable RSA `PrivateKeyPem` will be required at startup outside Development. Today missing issuer/audience values only warn outside Development, and an empty private key warns but cannot issue tokens or accept exchanges; an invalid configured key already fails startup. Stop issuing new links and drain outstanding links for the configured `Expiry` (seven days is only the default). Then set `Issuer` and `Audience` in Ante and AuthProxy at the same time (setting them in Ante is what turns the claim checks on; there is no separate switch), confirm that exchange validates, and resume issuance. Alternatively revoke and reissue outstanding invitations under the new configuration. **Enabling either issuer or audience validation already invalidates older links missing that claim**: waiting for their remaining lifetime *after* enabling it does not keep them working. See the [deployment cutover](./deployment.md#prepare-the-image-and-secrets).

## Identity and infrastructure

**Upgrade note:** invitation exchange and registration reject a sign-in whose identity provider cannot be resolved, instead of recording it with an empty provider. Exchange and subsequent requests use the same AuthProxy precedence. With canonical identity, AuthProxy sends `providerKey` and normalized `issuer` in the body and forwards `urn:cratis:identity:provider-key` / `urn:cratis:identity:issuer` on requests; Ante resolves only those two candidates (not raw legacy claims or Arc metadata). Without canonical identity, AuthProxy chooses one exchange value in order: `iss`, `identity_provider`, the Microsoft access-control-service identity-provider claim, then `AuthenticationType` (also forwarded as Arc provider metadata on requests). Ante selects that same first present value on requests, so a later configured claim cannot override an earlier unconfigured one. Without an identifying value, one configured provider can be inferred; with only an `AuthenticationTypes.Federation` marker, that sole provider must have an issuer. Startup warns when the provider list is empty, the only provider has no `Issuer`, or multiple providers include any issuer-bearing provider (a marker alone cannot distinguish those OIDC sign-ins). An unresolved exchange is also logged without personal or token data. Mirror the proxy's providers here, including each OIDC provider's `Issuer`, or configure canonical identity forwarding.

| Key | Type | Code default | Base / Development setting | When needed |
| --- | --- | --- | --- | --- |
| `IdentityProviders:Providers` | list of `{Name: string, Issuer: string}` | empty list (fields empty) | unset / unset | Mirror proxy providers for correct attribution, especially issuerless OAuth. Bind indexed entries such as `IdentityProviders__Providers__0__Name` and `IdentityProviders__Providers__0__Issuer`. No match may preserve an unknown reported name; ambiguous absence can resolve to empty. |
| `Cratis:Chronicle:ConnectionString` | Chronicle connection string | client default not set by Ante | unset / unset | Connect to Chronicle: Direct's Ante deployment sets `Cratis__Chronicle__ConnectionString`; deploy a compatible Chronicle 19 server; the recommended matched server/client release is 19.13.1. Check the connection compatibility result at startup. |
| `Cratis:MongoDB:Server` | connection string | framework default not established here | `mongodb://localhost:27017` / inherited | Point at reachable MongoDB for sessions and read models. |
| `Cratis:MongoDB:Database` | string | framework default not established here | `Ante` / inherited | Choose database for instance state; coordinate with Chronicle routing. |
| `Cratis:Arc:CorrelationId:HttpHeader` | string | framework default not established here | `X-Correlation-Id` / inherited | Override only with a matching gateway/host correlation policy. |
| `AllowedHosts` | ASP.NET Core host filter string | framework default not established here | `*` / inherited | Restrict for a deployment's hostnames; this is **not** a substitute for an authenticating proxy. |
| `ASPNETCORE_ENVIRONMENT` | environment name | host-dependent | unset / unset | `Development` exposes OpenAPI and loads Development settings; do not use for production. |
| `ANTE_PRIMEUI_LICENSE` | frontend build environment string | absent | supplied as CI build secret | PrimeReact license read by Vite at frontend build; manage as a build input, not an `Ante:*` runtime option. |

`Cratis` also reads other framework settings (including Chronicle connectivity); this table covers the keys defined by Ante's checked-in settings, not the entire Arc/Chronicle configuration surface. No `Ante:Locale` option exists.

## Routing limits and cutover

Changing `Ante:EventStore` or `Ante:Namespace` selects different local event/read-model routing; it does not move earlier events or guarantee that the destination has no history. `Ante:HostStores` selects *source* stores independently. For a separate lobby, set `Ante__EventStore=DirectLobby`, `Ante__Namespace=Default`, and `Ante__HostStores__0=Direct`, with its own MongoDB state. For Studio, set `Ante:HostStores` to `["Studio", "StudioAdmin"]` (the actual Chronicle store names, not `Core` and `Admin`). All configured stores belong to one trusted host product and share the same namespace as Ante; no tenant or namespace mapping is performed.

Before cutover, inventory existing subscriptions, observer ids and cursors; check destination history and observer registration, and route host observers to the renamed Ante store. The Direct reactor keeps id `Ante.Invitations.Receiving.IncomingInvitationReactor` and subscription id `Direct` to resume its cursor. Additional stores have source-qualified reactor ids and use their source name as subscription id. A custom rebuild whose legacy cursor belongs to a different source requires explicit operator mapping before rollout. Drain and reconcile removed sources and any facts written at the destination during rollback: configuration never moves history or resets cursors. See [Architecture](./architecture.md) and [Host integration](./host-integration.md).
