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

```
GET /healthz → 200 OK
```

registered directly in `Program.cs` as `app.MapGet("/healthz", () => Results.Ok())`. This is currently **unconditional** — it does not check MongoDB, the Chronicle connection, or anything else. A container reporting healthy does not guarantee the event store or database are reachable.

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

## Next steps

- [Configuration](./configuration.md) — the full settings reference.
- [Host Integration](./host-integration.md) — the contract a deployed instance's host product must speak.
