# Ante

> Ante (the initial stake in a poker game) is Cratis's SaaS invitation system: signed invitations, a lobby where invitees register, and event-based coordination with the host product.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Build](https://github.com/Cratis/Ante/actions/workflows/build.yml/badge.svg)](https://github.com/Cratis/Ante/actions/workflows/build.yml)
[![Publish](https://github.com/Cratis/Ante/actions/workflows/publish.yml/badge.svg)](https://github.com/Cratis/Ante/actions/workflows/publish.yml)
[![NuGet](https://img.shields.io/nuget/v/Cratis.Ante.Contracts?logo=nuget)](https://www.nuget.org/packages/Cratis.Ante.Contracts)
[![Discord](https://img.shields.io/discord/1182595891576717413?label=Discord&logo=discord&logoColor=white)](https://discord.gg/kt4AMpV8WV)

## The problem

Getting people into a SaaS product starts before they have an account. Someone has to be invited, the invitation has to be trustworthy, and the moment an invitee actually registers has to connect back to the invitation that brought them in. Doing this ad hoc in every product means re-solving the same questions each time: how invitations are issued, how they are verified, and how the rest of the system learns that an invitee arrived.

Ante's scope is exactly that slice — nothing more: **signed invitations**, a **lobby** where invitees register, and **event-based coordination** so the surrounding system can react to what happens.

## How Ante works

- **Signed invitations.** A host product appends an invitation event to its own Chronicle outbox; an inbox reactor in Ante picks it up, mints an RSA-signed JWT, and hands it back as `InvitationTokenIssued` — the host builds the link and emails it. Ante is the one place the signing key lives; no host reimplements the scheme.
- **The lobby.** A React SPA with three wizards — join an existing tenant, set up a new organization, self-service registration — driving Ante's generated command/query proxies directly, with an optional pluggable terms-and-conditions step.
- **Event coordination.** Everything crossing the boundary is a Chronicle event: invitations in, acceptances and registrations out. The host reacts to what Ante produces; Ante never calls back into the host directly.

## Status: v1

Ante v1 is implemented and buildable — a working .NET/Chronicle backend, a React lobby SPA, and specs for every slice (60 in-process backend specs, plus Vitest specs for the frontend's pure logic and the legal-acceptance component). It is not yet wired up as a deployed instance for any product — see [Deployment](./Documentation/deployment.md) for what that would take.

Extracted from the lobby inside [Cratis Studio](https://github.com/Cratis/Studio) and generalized into a standalone, reusable product — Studio-specific naming is gone, and Studio's own document-authoring/provisioning-confirmation machinery was not carried over. See [Boundaries](./Documentation/boundaries.md) for what Ante deliberately does not do.

### CI and publishing

CI builds, tests, lints, and type-checks on every push and pull request (`.github/workflows/build.yml`). Every pull request must carry a `major`/`minor`/`patch`/`no-release` label (`.github/workflows/verify-semver-label.yml`); merging a version-labelled one releases `ghcr.io/cratis/ante` and `Cratis.Ante.Contracts` on NuGet.org under a real semantic version, via [`cratis/release-action`](https://github.com/Cratis/release-action) (`.github/workflows/publish.yml`). See [Deployment](./Documentation/deployment.md) for details.

## Documentation

Full documentation — configuration reference, the invitation lifecycle, the host integration contract, the lobby wizards, deployment, and what Ante deliberately does not do — lives in [`Documentation/`](./Documentation/index.md). Start with [Getting Started](./Documentation/getting-started.md) to run it locally, or [Host Integration](./Documentation/host-integration.md) to wire a product up to it.

## Part of the Cratis ecosystem

Ante is a [Cratis](https://www.cratis.io) project, built on event sourcing with [Chronicle](https://github.com/Cratis/Chronicle) and CQRS with [Arc](https://github.com/Cratis/Arc) for ASP.NET Core, following the vertical-slice conventions in [`.ai/`](./.ai). The lobby SPA is built with [`@cratis/components`](https://github.com/Cratis/Components) on PrimeReact. Everything Cratis publishes today is MIT licensed and free to use — join the community on [Discord](https://discord.gg/kt4AMpV8WV).

## License

Ante is licensed under the [MIT License](LICENSE) and free to use.
