# Ante

> Ante (the initial stake in a game) is your SaaS invitation system: create signed invitations, manage the lobby where invitees register, and coordinate via events.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## The problem

Getting people into a SaaS product starts before they have an account. Someone has to be invited, the invitation has to be trustworthy, and the moment an invitee actually registers has to connect back to the invitation that brought them in. Doing this ad hoc in every product means re-solving the same questions each time: how invitations are issued, how they are verified, and how the rest of the system learns that an invitee arrived.

Ante's scope is exactly that slice: **signed invitations**, a **lobby** where invitees register, and **event-based coordination** so the surrounding system can react to what happens.

## Status: v1

Ante v1 is implemented and buildable — a working .NET/Chronicle backend, a React lobby SPA, and specs for every slice. It is not yet wired up as a deployed instance for any product (see [What's not here yet](#whats-not-here-yet)).

Extracted from the lobby inside [Cratis Studio](https://github.com/Cratis/Studio), generalized into a standalone, reusable product: Studio-specific naming is gone (`StudioUrl` → `HostAppUrl`, `IdentityProvider` → `IdentityProviderName` to avoid colliding with a type Cratis.Arc.Identity already ships), and Studio's own document-authoring/provisioning-confirmation machinery was not carried over — see [What Ante does not do](#what-ante-does-not-do).

### What's implemented

- **Signed invitations.** A host product appends `UserInvitedToJoinTenant` / `UserInvitedToCreateTenant` / `InvitationRevoked` to its own Chronicle outbox. Ante's inbox reactor picks them up, and Ante mints an RSA-signed JWT (`jti` = invitation id, `invite_type` claim) and forwards it to its own outbox as `InvitationTokenIssued` — the host builds the invitation link from that token and emails it. Ante is the **one** place the signing key lives; no host reimplements the scheme.
- **Invite exchange.** `_invite/exchange` — called by an authentication proxy after OIDC login completes with the invite token — validates the token and records an accepted-invitation session, which an `InvitationIdentityProvider` resolves into `InvitationIdentityDetails` for the rest of the request pipeline.
- **Join-tenant acceptance.** `AcceptInvitation` command: resolves the invitee's identity, optionally checks an identity backchannel for a pre-flight uniqueness signal, appends `InvitationToJoinTenantAccepted`.
- **Create-organization setup.** `SetupOrganization` command: validates the organization name (rejects characters that would break a downstream Chronicle namespace), enforces uniqueness with a race-safe constraint, appends `InvitationToCreateTenantAccepted`.
- **Self-service registration.** `RegisterOrganization` command for a host's `/register` entry point — no invitation, identity comes from the current request.
- **Pluggable legal acceptance.** A host registers its own `ILegalDocumentSource`; when it does, the three wizards show a terms-and-conditions step and append `LegalTermsAccepted`. When it doesn't (the default), there is nothing to show and the step is skipped entirely — Ante ships no bundled legal text and no document-versioning pipeline.
- **The lobby SPA.** Three wizards (join an existing tenant, set up a new organization, self-service registration) built with `@cratis/components` on PrimeReact, driving the generated command/query proxies directly.
- **Single-tenant.** Everything above runs in Chronicle's `Default` namespace, matching the lobby's own original design — Ante does not manage multiple tenants of its own.
- **Specs for every slice**: `CommandScenario`/`ReadModelScenario`/`ReactorScenario` in-process specs for the backend (60 specs), Vitest specs for the frontend's pure logic and the legal-acceptance component.

### Configuration reference

All configuration lives under the `Ante` section (or the matching `Ante__*` environment variables).

| Key | Default | Purpose |
|---|---|---|
| `Ante:EventStore` | `Ante` | The Chronicle event store this instance runs against. **Never hardcoded** — a second instance in the same cluster is just a different value here. |
| `Ante:HostAppUrl` | _(empty)_ | Base URL of the host application. The wizards redirect here once an invitation is accepted or a registration completes. Supports a `{tenant}` placeholder. |
| `Ante:LogoUrl` | _(empty)_ | Custom logo shown in the lobby. Empty renders a plain "Ante" wordmark. |
| `Ante:CustomCssUrl` | _(empty)_ | Custom CSS to inject into the lobby. |
| `Ante:IdentityBackchannelUrl` | _(empty)_ | Base URL of a host endpoint Ante calls to pre-flight-check whether an identity is already associated with a user. Empty skips the check (the host's own uniqueness constraint is always the authoritative guard). |
| `Ante:Invitations:Token:PrivateKeyPem` | _(empty)_ | PEM-encoded RSA private key invitation JWTs are signed with. Required for token issuance to work. |
| `Ante:Invitations:Token:PublicKeyPem` | _(empty)_ | PEM-encoded RSA public key, published for hosts/proxies that verify independently. |
| `Ante:Invitations:Token:Issuer` / `Audience` | _(empty)_ | Optional `iss`/`aud` claims. Left empty, no such claim is validated. |
| `Ante:Invitations:Token:Expiry` | `7.00:00:00` | How long an issued invitation token remains valid. |
| `IdentityProviders:Providers` | _(empty)_ | Mirrors the identity providers a fronting authentication proxy is configured with, so a sign-in that carries no `iss` (an OAuth2-only provider) can still be attributed correctly. |

**A "Direct" reference instance** — the one this extraction was validated against — runs with:

```
Ante__EventStore=DirectLobby
```

The inbox source store name (which host store Ante's inbox cross-subscribes to) is a **compile-time constant**, not a configuration value — see [Known limitation: the inbox source store](#known-limitation-the-inbox-source-store) below. It defaults to `"Direct"`.

### Known limitation: the inbox source store

`Source/Ante/Invitations/Receiving/InboxSourceStore.cs` names the Chronicle event store Ante's inbox reactor cross-subscribes to for the host's `UserInvitedToJoinTenant` / `UserInvitedToCreateTenant` / `InvitationRevoked` events. This is a **compile-time literal, not a runtime configuration value** — and that is a known limitation, not a design choice.

Chronicle's `[EventStore]` attribute is the only mechanism for pointing an observer at a store other than its own, and C# requires its constructor argument to be a compile-time constant. Investigated for this extraction against the Chronicle 16.44.1 / 17.0.0 XML documentation: `EventStoreAttribute` is confirmed as the sole mechanism — there is no fluent or runtime-registered equivalent in the shipped `Cratis.Chronicle` client API. Until one exists, an Ante deployment that needs a source store other than `"Direct"` has to change the constant in that one file and rebuild.

The literal is deliberately isolated to that single file, with a comment explaining why. **An upstream issue belongs on `Cratis/Chronicle`** describing the missing capability (a runtime/config-driven way to register a cross-store observer); this repository does not file it.

### What Ante does not do

Deliberately out of scope, and staying that way:

- **Sending email.** Ante mints tokens and appends `InvitationTokenIssued`; a host builds the actual invitation link and sends it.
- **Legal document authoring/versioning.** No admin UI for writing terms and conditions, no revision history, no bundled fallback text. A host wanting the legal step supplies an `ILegalDocumentSource` from wherever it manages that content.
- **Provisioning.** Seats, trials, billing, tenant databases — Ante's job ends the moment it appends an accepted/registered event to its own outbox. A host reacts to that event to actually provision anything. There is consequently no "waiting for provisioning" UI state in the wizards (Ante's original source material had a multi-minute wait/retry state machine for exactly that; it doesn't apply here because there is nothing external to wait for).
- **Admin invite-authoring UI.** Deciding *who* to invite, with what role, is a host concern.
- **Multi-tenancy of Ante itself.** Ante runs single-tenant; a product needing several isolated lobbies runs several Ante instances (see `Ante:EventStore` above).

## What remains for a deployable instance

This v1 is a buildable, spec-covered application — it is not yet an operable deployment:

- **Deployment wiring** (Kubernetes manifests/Pulumi/Helm, secret provisioning for the RSA signing key, an actual `Ante:HostAppUrl` and `IdentityProviders` configuration for a real host) does not exist here — it belongs to whatever deploys the "Direct" reference instance, or any other host's instance.
- **CI** builds, tests, lints, and type-checks on every push and pull request (`.github/workflows/build.yml`). Merges to `main` additionally build and push `ghcr.io/cratis/ante` (`.github/workflows/publish.yml`) — `Source/Ante/Dockerfile` publishes the backend as a portable (RID-less) `dotnet publish` output plus the built SPA, matching the pattern used elsewhere in the Cratis ecosystem. Versioning is currently `0.1.<CI run number>`, not yet a real semantic version — this repo has no release-action scaffolding (label-driven semver, GitHub releases) to plug into yet.

## Part of the Cratis ecosystem

Ante is a [Cratis](https://www.cratis.io) project, built on event sourcing with [Chronicle](https://github.com/Cratis/Chronicle) and CQRS with [Arc](https://github.com/Cratis/Arc) for ASP.NET Core, following the vertical-slice conventions in `.ai/`. The frontend is a React SPA on [`@cratis/components`](https://github.com/Cratis/Components) over PrimeReact.

## License

Ante is licensed under the [MIT License](LICENSE) and free to use.
