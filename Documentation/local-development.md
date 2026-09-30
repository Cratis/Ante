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

`Integration/Ante.Integration.Specs.csproj` hosts Ante's real `Program` in-process against a Chronicle kernel with its embedded MongoDB, started in Docker, and gives each specification its own Ante store, host stores and read-model database. A minimal host store publishes invitations to its outbox and receives Ante's contracts in `inbox-{ante store}`. The specifications cover token issuance back to the host, rejection of a non-canonical id, id reuse after revocation, join and organization setup with legal acceptance, self-registration, PII read back decrypted in the host inbox, two host stores configured together, and one onboarding per journey followed from the host outbox (or Ante's log, for self-registration) to the host's receipt, comparing complete payloads, event counts, event source ids, correlation ids and compliance subjects at every hop.

The specifications under `Integration/Replay` build a store that has seen every kind of onboarding and then work on it through the kernel's Observers service, the same service the Workbench and the `cratis chronicle` CLI use. They replay the five forwarding reactors and the token-issuing reactor, and remove those reactors' observers while Ante is stopped, so that their checkpoints are lost and they observe the event log from the start again. In both cases Ante's outbox and the host's inbox must stay the same, event for event. The specifications also replay every onboarding projection and check that each read model comes back with the same documents and the same released personal data. After the rebuild, claimed organization names are still refused, both by the registration command and at the event store, and released names are still free.

The project is not part of `Ante.slnx`, so the backend commands above never need Docker. With Docker running, from the repository root:

```bash
docker pull cratis/chronicle:19.23.0-development
dotnet test Integration/Ante.Integration.Specs.csproj
```

A run takes a few minutes and removes its container afterwards. Three environment variables help when something fails:

| Variable | Effect |
| --- | --- |
| `ANTE_CHRONICLE_IMAGE` | Kernel image to run instead of `cratis/chronicle:19.23.0-development`, for example `cratis/chronicle:19.4.7-development`. |
| `ANTE_INTEGRATION_KEEP_CONTAINER=true` | Leaves the container running for inspection, for example with `docker exec <id> mongosh`. Remove it yourself afterwards. |
| `ANTE_INTEGRATION_DEBUG_LOG` | A logging category, such as `Cratis`, to log at Debug level. |

The fixture does not cover the authentication proxy's token verification or host provisioning; the frontend is covered by the [browser end-to-end specifications](#run-the-browser-end-to-end-specifications). The command posts use the forwarded identity headers the proxy would set.

These specifications are the Tier 2 check. Run them locally before opening a pull request, or before marking one ready for review, when the change touches Chronicle wiring (event stores, sequences, inbox/outbox routing, compliance), contracts, reactors or projections. The in-process specifications substitute the sink and the kernel, so a read model can pass there and still never be populated by a real kernel.

They do not run on every push. `.github/workflows/integration.yml` runs them nightly against `cratis/chronicle:latest-development`, on demand from the Actions tab (pick the image with the `chronicle-image` input), and on a pull request that carries the `run-integration` label. Adding the label starts a run, and while it stays on the pull request every new push runs them again; remove it when the extra runs are no longer needed.

## Run the browser end-to-end specifications

`E2E/` drives the lobby's three journeys - join by invitation, invited organization setup and self-service registration - in Chromium with Playwright, and runs axe on every wizard step. It covers the happy paths with and without legal consent, editable validation errors, reloading and a second tab, keyboard-only operation and focus visibility, live-region announcements, 200% zoom and a 320-pixel screen, reduced motion, forced colors with high contrast, English and Norwegian Bokmål, and the render recovery notice.

Playwright starts `E2E/Host`, a small .NET program that reuses the fixture above: one Chronicle kernel in Docker, and two real Ante instances served over Kestrel with the built frontend - `plain` on port 5611 without legal documents and `legal` on 5612 with a host-provided set - each wired to a minimal host store. Port 5610 is a control endpoint the specifications use to publish invitations and read what the host received, and it also stands in for the host application Ante hands over to. The host plays the authentication proxy: a browser carrying the `ante-e2e-subject` cookie reaches Ante with the forwarded identity headers for that subject on every request, Server-Sent Events included. Set `ANTE_E2E_PORT` to move all three ports; `ANTE_CHRONICLE_IMAGE` works as for the fixture.

The lobby does not render without a PrimeUI license, so building the frontend needs `ANTE_PRIMEUI_LICENSE`, from the environment or the repository root's `.env`. With Docker running, from the repository root:

```bash
corepack enable
yarn install --immutable
cd E2E
yarn install-browsers
yarn e2e
```

`yarn e2e` builds the frontend into `Source/Ante/wwwroot` and runs the suite; `yarn test` runs it against the frontend already built. Outside CI, a host you already started with `dotnet run --project Host/Ante.E2E.Host.csproj` is reused, which keeps reruns quick. Without the team's license, `ANTE_E2E_ALLOW_UNLICENSED_PRIMEUI=1 yarn e2e` builds with a placeholder key, and PrimeUI shows its invalid-license banner on every page; that is enough to run the suite locally, but it is not the licensed build CI verifies.

axe violations of serious or critical impact fail the run, except the ones listed in `E2E/support/accessibility.ts`, each tied to an open issue by rule and element. Every violation, listed or not, is attached to its test in the HTML report. Add to the list only with an issue, and remove an entry together with its fix.

The `Browser end-to-end` job in `.github/workflows/integration.yml` runs the suite on the same triggers as the container-backed specifications, with the `PRIMEUI_LICENSE` secret. A pull request from a fork or from Dependabot does not receive the secret, so the job is skipped there with a notice. A failed run uploads the Playwright report and traces as the `browser-e2e-report` artifact.

## Run a development server

With MongoDB and Chronicle independently configured:

1. From `Source/Ante`, run `dotnet run --urls http://localhost:5002`. The checked-in launch profile otherwise declares port 5050.
2. From `Source/Ante` in another terminal, run `yarn dev`. Vite serves the SPA on port 9002 and forwards `/api`, `/.cratis` and `/scalar` to 5002.
3. If you need Development OpenAPI, change Vite's `/openapi` proxy target from **5000** to your backend port. Check `/healthz/ready` for MongoDB connectivity; it does not check Chronicle.

Running the SPA without a host, invite token and proxy is not an end-to-end test. The backend's Development configuration includes `http://{tenant}.localhost:8090/` as a sample host destination, not a provisioned host. For a deployed network and trust boundary see [Deployment](./deployment.md).
