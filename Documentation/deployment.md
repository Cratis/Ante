---
title: Deployment
description: The Ante container image, health endpoint, required and optional configuration, generating an RSA signing keypair, and what deployment tooling does not yet exist.
---

## The image

Merging a pull request labelled `major`, `minor` or `patch` builds and pushes `ghcr.io/cratis/ante` (`.github/workflows/publish.yml`). The image:

- Is built `FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble`.
- Listens on port **8080** (`EXPOSE 8080`).
- Contains a portable, RID-less `dotnet publish` output copied in as `out` — nothing is compiled inside the Docker build.
- Has `appsettings.Development.json` removed (`RUN rm -f appsettings.Development.json`) before the entrypoint, so the throwaway development signing keypair never ships in a container image.
- Is tagged with the real semantic version [`cratis/release-action`](https://github.com/Cratis/release-action) resolves from the merged pull request's `major`/`minor`/`patch` label (e.g. `1.4.0`), plus `latest` for the most recent non-prerelease version.

## Health check

Liveness and readiness are two separate endpoints, on purpose — a dependency outage must change whether
an instance receives traffic, never whether an orchestrator considers the process itself alive:

```
GET /healthz       → 200 OK, unconditionally
GET /healthz/ready → 200 OK  when every readiness dependency check passes
                    → 503    when any of them fails or times out
```

- **`/healthz` (liveness)** is preserved exactly as it always has been: `AnteHealthChecks.MapAnteHealthChecks`
  (`Program.cs`) maps it with zero checks (`Predicate = _ => false`), so it can never be dragged down by
  MongoDB, Chronicle, or anything else being unreachable. Point a container orchestrator's *liveness*
  probe here — a failure means "restart the process", and a dependency being temporarily down is never a
  reason to do that.
- **`/healthz/ready` (readiness)** runs every health check tagged `"ready"` — currently a MongoDB
  connectivity ping (`MongoDbHealthCheck`) — each individually bounded by `AnteHealthChecks.DependencyTimeout`
  (3 seconds), so one stuck dependency can never hang the whole probe or accumulate blocked work behind
  it. Point a container orchestrator's *readiness* probe here — a failure means "stop routing traffic to
  this instance", not "restart it". The response body is the health check middleware's own default
  writer: a single low-cardinality status word (`Healthy`, `Degraded`, or `Unhealthy`) and nothing else —
  no connection strings, exception messages, or stack traces from the failing dependency ever reach the
  response (see `MongoDbHealthCheck`).

A request under `/api`, `/openapi`, `/_invite` or `/healthz` that matches none of the routes above (a
typo, or a path this build never mapped) gets a genuine `404` from `ApiRouteGuard` rather than falling
through to the single-page application (SPA) shell — see [Guarded routes](#guarded-routes) below.

## Guarded routes

- **The generated OpenAPI document (`/openapi/...`) is exposed only in Development** (`ConditionalOpenApi`,
  `Program.cs`). It is a full map of Ante's command/query surface, which a non-development deployment
  must never publish to an unauthenticated caller. A request for it outside Development falls through to
  `ApiRouteGuard` and gets `404`, the same as any other unmapped path under a reserved prefix.
- **`/api`, `/openapi`, `/_invite` and `/healthz` are reserved prefixes.** Anything under one of them that
  matches no real endpoint answers `404` (`ApiRouteGuard.MapReservedPrefixGuards`) instead of the SPA
  shell — a caller probing for API surface, or a client following a stale or misspelled path, gets an
  honest error rather than a misleading `200` with `index.html`.
- **`/_invite/exchange`** (`InviteExchangeBypassMiddleware`) validates the bearer token carried by the
  authenticating proxy's exchange request itself — an invalid, expired, or missing token is rejected
  before any session is recorded. It runs ahead of routing so it works even before authorization has
  resolved an identity for the request.
- **Everything else under `/api`** is Ante's own onboarding surface — the SPA shell, its static assets,
  and public onboarding pages are reachable without prior authentication by design (self-registration
  "must never require invitation staging" — see the [epic](https://github.com/Cratis/Ante/issues/10)).
  Invitation-bound commands (`AcceptInvitation`, `SetupOrganization`) are authorized in application code
  against the caller's own accepted-invitation session (`ISignedInIdentity.IsVerifiedOwnerOf`) rather than
  through an ASP.NET Core `[Authorize]` policy — knowing an invitation id is never enough to act on it.

## Private diagnostics

The identity backchannel's own outage warning (`IdentityBackchannelLogging.LogIdentityBackchannelUnavailable`)
carries no organization name, subject, or any other onboarding-specific value — only the exception itself,
which an operator needs to diagnose the outage. This is deliberate: a warning this shape can legitimately
fire on every request while a host's backchannel is down, and the value it would otherwise name is exactly
the kind of onboarding-specific fact private diagnostics must never surface.

## Required configuration

An instance cannot do anything useful without:

| Setting | Why |
|---|---|
| `Ante:EventStore` | Which Chronicle store this instance runs against — defaults to `Ante`, but a real deployment typically names it after the product/lobby it serves |
| `Ante:HostAppUrl` | Where the wizards redirect once onboarding completes |
| `Ante:Invitations:Token:PrivateKeyPem` / `PublicKeyPem` | Without these, no invitation token can ever be issued |

Everything else in [Configuration](./configuration.md) has a usable empty default.

## The RSA signing keypair

Generate a real keypair for any non-development deployment — never reuse the one committed in `appsettings.Development.json`:

```bash
openssl genrsa -out ante-private.pem 2048
openssl rsa -in ante-private.pem -pubout -out ante-public.pem
```

Supply the private key's contents as `Ante__Invitations__Token__PrivateKeyPem` and the public key's as `Ante__Invitations__Token__PublicKeyPem` (PEM text, newlines included) — as a mounted secret or a secret-manager-backed environment variable, never committed to source control. Publish the public key to whichever hosts or authentication proxies need to verify tokens independently of Ante.

## NuGet package

`Cratis.Ante.Contracts` — the event contract types described in [Host Integration](./host-integration.md) — is packed and pushed to [NuGet.org](https://www.nuget.org/packages/Cratis.Ante.Contracts) whenever a pull request carrying a `major`/`minor`/`patch` label merges to `main`, via trusted publishing (OIDC, no long-lived API key). It carries the same version as the image and the GitHub release. A host product references this package directly rather than redefining the event shapes.

## Versioning and releases

Every pull request must carry exactly one of `major`, `minor`, `patch` or `no-release` before it can merge (`.github/workflows/verify-semver-label.yml`). On merge, `.github/workflows/publish.yml` uses [`cratis/release-action`](https://github.com/Cratis/release-action) to work out the next semantic version from that label, cut a GitHub release, and tag the image and the NuGet package with it. A pull request labelled `no-release` merges without publishing anything — the correct outcome for changes with no outward effect (documentation, CI, tests).

## What is not yet provided

This is a buildable, spec-covered application — it is not yet a turnkey operable deployment:

- **No Kubernetes manifests, Helm charts, or Pulumi program.** Standing up an instance (secret provisioning for the signing key, the actual `Ante:HostAppUrl` and `IdentityProviders` for a real host) is left to whatever deploys it.
- **No key rotation tooling.** Rotating the signing keypair is a manual operation today — issue new tokens with the new key, publish the new public key, and accept that outstanding unaccepted invitations signed with the old key remain valid until they expire.
- **No startup validation of trust settings** (key correspondence/strength, signing algorithm, audience/provider rules, session expiry). `AnteRoutingValidator` only covers store/namespace routing today. The trust and compatibility contract these settings would be validated against is not yet agreed — see [WP-00, #11](https://github.com/Cratis/Ante/issues/11) — so validating them now would mean inventing rules ahead of that agreement rather than enforcing one.
- **No independent observer/publication-lag readiness indicator.** `/healthz/ready` currently reflects MongoDB connectivity only; a signal for "the outbox-forwarding reactors are falling behind" needs durable publication and progress ([WP-05, #16](https://github.com/Cratis/Ante/issues/16)) to define what "lag" means before it can be measured.
- **Route guarding does not yet integrate owner authorization end-to-end.** `/api` command endpoints are guarded at the application layer today (`ISignedInIdentity.IsVerifiedOwnerOf`); a full authorized-route matrix depends on [WP-02, #13](https://github.com/Cratis/Ante/issues/13).

## Next steps

- [Configuration](./configuration.md) — the full settings reference.
- [Host Integration](./host-integration.md) — the contract a deployed instance's host product must speak.
