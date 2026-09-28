---
title: Work on Ante locally
description: Run the repository's exact backend and frontend checks and understand local runtime prerequisites.
---

The repository includes an end-to-end fixture with a real Chronicle kernel, MongoDB and a minimal host store (see [Run the end-to-end specifications](#run-the-end-to-end-specifications)), but no verifying proxy. You can build and test the source without it; to exercise a complete journey through a proxy, provision one and use [Host integration](./host-integration.md). The committed Development RSA keypair is throwaway and must never be reused outside local development.

## Check backend and frontend

CI uses .NET 10 and Node 22. From the repository root, run its exact backend commands:

```bash
dotnet build Ante.slnx --configuration Debug
dotnet build Ante.slnx --configuration Release -p:CratisProxiesOutputPath=
dotnet test Ante.slnx --configuration Debug --no-build --logger "console;verbosity=minimal"
```

The Debug build regenerates TypeScript proxies and compiles colocated backend specifications. When intentionally adding a public `[EventType]` or generation, update the committed snapshot of Ante's camelCase Chronicle wire schemas from the repository root:

```bash
ANTE_UPDATE_EVENT_SCHEMA_SNAPSHOT=1 dotnet test Source/Ante.Contracts/Ante.Contracts.csproj --filter FullyQualifiedName~and_comparing_the_snapshot
```

Review and commit `Source/Ante.Contracts/EventSchemas.snapshot.json`, then rerun tests without the variable. The guard refuses to overwrite schemas for released generations listed in `Source/Ante.Contracts/ReleasedEventSchemas.json`, even in update mode; bump a released generation and add an `EventTypeMigration` instead. Unreleased generations can still change. Merging to `main` publishes a release (`.github/workflows/publish.yml`), so any pull request that adds an event type or generation must also add it to `ReleasedEventSchemas.json` before merge. The Release build deliberately disables a second proxy generation. To match CI's frontend job, from the repository root run:

```bash
dotnet build Ante.slnx --configuration Debug
corepack enable
yarn install --immutable
cd Source/Ante
yarn lint:ci
yarn compile
yarn test
yarn build
```

CI supplies `ANTE_PRIMEUI_LICENSE` as a secret for `yarn build`; Vite reads it in `.frontend/index.tsx` and passes it to PrimeReact. A local build without a valid license is not a verified equivalent of that CI build. Use `lint:ci` for checking: `yarn lint` includes `--fix` and changes files. Specs and frontend helpers do not replace a cross-system invitation test. When editing wizard JSX, keep `StepperPanel` as a direct child expression of `CommandStepper`; wrapping a panel in a component that returns it breaks step detection (see the comment in `Invitations/UserSetup/UserSetupPage.tsx`).

## Run the end-to-end specifications

`Integration/Ante.Integration.Specs.csproj` hosts Ante's real `Program` in-process against a Chronicle kernel with its embedded MongoDB, started in Docker, and gives each specification its own Ante store, host stores and read-model database. A minimal host store publishes invitations to its outbox and receives Ante's contracts in `inbox-{ante store}`. The specifications cover token issuance back to the host, rejection of a non-canonical id, id reuse after revocation, join and organization setup with legal acceptance, self-registration, PII read back decrypted in the host inbox, and two host stores configured together.

The project is not part of `Ante.slnx`, so the backend commands above never need Docker. With Docker running, from the repository root:

```bash
docker pull cratis/chronicle:19.13.1-development
dotnet test Integration/Ante.Integration.Specs.csproj
```

A run takes a few minutes and removes its container afterwards. Three environment variables help when something fails:

| Variable | Effect |
| --- | --- |
| `ANTE_CHRONICLE_IMAGE` | Kernel image to run instead of `cratis/chronicle:19.13.1-development`, for example `cratis/chronicle:19.4.7-development`. |
| `ANTE_INTEGRATION_KEEP_CONTAINER=true` | Leaves the container running for inspection, for example with `docker exec <id> mongosh`. Remove it yourself afterwards. |
| `ANTE_INTEGRATION_DEBUG_LOG` | A logging category, such as `Cratis`, to log at Debug level. |

The fixture does not cover the authentication proxy's token verification, host provisioning or the frontend. The command posts use the forwarded identity headers the proxy would set.

These specifications are the Tier 2 check. Run them locally before opening a pull request, or before marking one ready for review, when the change touches Chronicle wiring (event stores, sequences, inbox/outbox routing, compliance), contracts, reactors or projections. The in-process specifications substitute the sink and the kernel, so a read model can pass there and still never be populated by a real kernel.

They do not run on every push. `.github/workflows/integration.yml` runs them nightly against `cratis/chronicle:latest-development`, on demand from the Actions tab (pick the image with the `chronicle-image` input), and on a pull request that carries the `run-integration` label. Adding the label starts a run, and while it stays on the pull request every new push runs them again; remove it when the extra runs are no longer needed.

## Run a development server

With MongoDB and Chronicle independently configured:

1. From `Source/Ante`, run `dotnet run --urls http://localhost:5002`. The checked-in launch profile otherwise declares port 5050.
2. From `Source/Ante` in another terminal, run `yarn dev`. Vite serves the SPA on port 9002 and forwards `/api`, `/.cratis` and `/scalar` to 5002.
3. If you need Development OpenAPI, change Vite's `/openapi` proxy target from **5000** to your backend port. Check `/healthz/ready` for MongoDB connectivity; it does not check Chronicle.

Running the SPA without a host, invite token and proxy is not an end-to-end test. The backend's Development configuration includes `http://{tenant}.localhost:8090/` as a sample host destination, not a provisioned host. For a deployed network and trust boundary see [Deployment](./deployment.md).
