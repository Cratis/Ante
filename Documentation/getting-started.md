---
title: Getting started
description: Run Ante's backend and lobby SPA locally, with the throwaway development signing keypair and the minimum configuration needed to see it come up.
---

## Prerequisites

- .NET 10 SDK
- Node.js 20+ and Yarn 4 (via [Corepack](https://nodejs.org/api/corepack.html): `corepack enable`)
- A MongoDB instance reachable at `mongodb://localhost:27017` (the default for `Cratis:MongoDB:Server`)
- A Chronicle event store Ante's Chronicle client can connect to. Ante does not run an event store inside its own process — it connects over gRPC to a Chronicle instance running on its own (typically the `cratis/chronicle` container). See Chronicle's own documentation for running a store locally.

## The development signing keypair

`Source/Ante/appsettings.Development.json` ships a throwaway RSA keypair under `Ante:Invitations:Token`, marked with an explicit `_comment` warning:

:::caution
This keypair is committed to source control and is **never safe to use outside local development**. It signs the invitation JWTs Ante issues — anyone who has it can forge an invitation. Every real deployment must generate and supply its own keypair; see [Deployment](./deployment.md#the-rsa-signing-keypair).
:::

The Dockerfile removes this file (`rm -f appsettings.Development.json`) before the image is finalized, so it never ships in a container.

## Run the backend

```bash
cd Source/Ante
dotnet run --urls http://localhost:5002
```

The project's committed launch profile (`Properties/launchSettings.json`) listens on `http://localhost:5050` by default. Override the URL as shown above so it matches the port the frontend dev server expects — see the caution below.

A Debug build regenerates the TypeScript command/query proxies the lobby SPA imports, so run the backend at least once before starting the frontend.

## Run the lobby SPA

```bash
cd Source/Ante
yarn install
yarn dev
```

This starts the Vite dev server on `http://localhost:9002`.

:::caution
The checked-in `vite.config.ts` proxies `/api`, `/.cratis`, and `/scalar` to `http://localhost:5002`, and `/openapi` to `http://localhost:5000` — neither matches the backend's own default launch profile port (`5050`). Run the backend with `--urls http://localhost:5002` (as shown above) so the SPA's API calls actually reach it, or edit the proxy targets in `Source/Ante/.frontend/vite.config.ts` to match whatever port you run the backend on.
:::

Open `http://localhost:9002`. Without an invitation token or a signed-in identity, you will see the join-tenant wizard shell waiting on an invitation — a full trial of an onboarding journey needs a host product appending invitation events and an authentication proxy in front (see [Host Integration](./host-integration.md)).

## Minimal configuration to try a flow end to end

At minimum, set:

```json
{
  "Ante": {
    "EventStore": "Ante",
    "HostAppUrl": "http://{tenant}.localhost:8090/",
    "Invitations": {
      "Token": {
        "PrivateKeyPem": "...",
        "PublicKeyPem": "..."
      }
    }
  }
}
```

`EventStore` and the token keypair are the only settings with no usable default outside the throwaway development one. Everything else has a safe empty default. See [Configuration](./configuration.md) for the full reference, including the `Ante__*` environment-variable form used in containers.

## Next steps

- [Configuration](./configuration.md) — every setting, its default, and its effect.
- [Invitation Lifecycle](./invitation-lifecycle.md) — what happens between a host appending an invitation event and an invitee landing back in the host application.
- [Host Integration](./host-integration.md) — what a host product needs to emit, consume, and front with an authentication proxy.
