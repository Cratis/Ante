# Ante

> Ante (the initial stake in a game) is your SaaS invitation system: create signed invitations, manage the lobby where invitees register, and coordinate via events.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## The problem

Getting people into a SaaS product starts before they have an account. Someone has to be invited, the invitation has to be trustworthy, and the moment an invitee actually registers has to connect back to the invitation that brought them in. Doing this ad hoc in every product means re-solving the same questions each time: how invitations are issued, how they are verified, and how the rest of the system learns that an invitee arrived.

Ante's scope is exactly that slice: **signed invitations**, a **lobby** where invitees register, and **event-based coordination** so the surrounding system can react to what happens.

## Status: v1

Ante v1 is implemented and buildable — a working .NET/Chronicle backend, a React lobby SPA, and specs for every slice. It is not yet wired up as a deployed instance for any product (see [Deployment](./Documentation/deployment.md)).

Extracted from the lobby inside [Cratis Studio](https://github.com/Cratis/Studio), generalized into a standalone, reusable product: Studio-specific naming is gone (`StudioUrl` → `HostAppUrl`, `IdentityProvider` → `IdentityProviderName` to avoid colliding with a type Cratis.Arc.Identity already ships), and Studio's own document-authoring/provisioning-confirmation machinery was not carried over — see [Boundaries](./Documentation/boundaries.md).

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

### CI and publishing

CI builds, tests, lints, and type-checks on every push and pull request (`.github/workflows/build.yml`). Every pull request must carry a `major`/`minor`/`patch`/`no-release` label (`.github/workflows/verify-semver-label.yml`); merging a version-labelled one releases `ghcr.io/cratis/ante` and `Cratis.Ante.Contracts` on NuGet.org under a real semantic version, via [`cratis/release-action`](https://github.com/Cratis/release-action) (`.github/workflows/publish.yml`). See [Deployment](./Documentation/deployment.md) for details.

## Documentation

Full documentation — configuration reference, the invitation lifecycle, host integration contract, the lobby wizards, deployment, and what Ante deliberately does not do — lives in [`Documentation/`](./Documentation/index.md).

## Part of the Cratis ecosystem

Ante is a [Cratis](https://www.cratis.io) project, built on event sourcing with [Chronicle](https://github.com/Cratis/Chronicle) and CQRS with [Arc](https://github.com/Cratis/Arc) for ASP.NET Core, following the vertical-slice conventions in `.ai/`. The frontend is a React SPA on [`@cratis/components`](https://github.com/Cratis/Components) over PrimeReact.

## License

Ante is licensed under the [MIT License](LICENSE) and free to use.
