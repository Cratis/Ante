---
title: Work on Ante locally
description: Run the repository's exact backend and frontend checks and understand local runtime prerequisites.
---

The repository includes container-backed [end-to-end specifications](#run-the-end-to-end-specifications) and a [standalone first-invitation fixture](#run-the-first-invitation-fixture) with a real Ante, Chronicle, MongoDB, a host stub and a stub signing/validating proxy. You can build and test the source without containers; use [Host integration](./host-integration.md) when connecting a real host and proxy. The committed Development RSA keypair is throwaway and must never be reused outside local development.

## Check backend and frontend

CI uses .NET 11 and Node 22. From the repository root, run its exact backend commands:

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

## Run the first-invitation fixture

`Fixture/compose.yml` runs Ante built from this revision with Chronicle, a MongoDB replica set, a host stub and a stub signing/validating proxy. The proxy includes a stub sign-in form: it is **not** AuthProxy or a real identity provider/proxy, and this is not a production topology. Connecting a real provider remains separate work ([#123](https://github.com/Cratis/Ante/issues/123)). The `Fixture first invitation` CI job runs the API journey without the licensed SPA, alongside the integration and browser jobs.

Prepare artifacts and images while online, from the repository root with .NET 11, Node 22 and Docker Compose available:

```bash
dotnet publish Source/Ante/Ante.csproj --configuration Release --self-contained false -p:UseAppHost=false -p:CratisProxiesOutputPath= -o Source/Ante/out
dotnet publish Fixture/Host/Ante.Fixture.Host.csproj --configuration Release --self-contained false -p:UseAppHost=false -p:CratisProxiesOutputPath= -o Fixture/Host/out
dotnet publish Fixture/Proxy/Ante.Fixture.Proxy.csproj --configuration Release --self-contained false -p:UseAppHost=false -p:CratisProxiesOutputPath= -o Fixture/Proxy/out
node Fixture/compose-policy.mjs
docker compose -f Fixture/compose.yml pull --ignore-buildable
docker compose -f Fixture/compose.yml build
```

Start without pulling or building anything, then run the first invitation:

```bash
docker compose -f Fixture/compose.yml up -d --wait --pull never --no-build
node Fixture/first-invitation.mjs --timeout 300
```

The backend network is `internal: true`: Ante, Chronicle, MongoDB and the host have no outbound internet access and publish no ports. Only the proxy joins the edge network and publishes `127.0.0.1:8080`. CI also probes backend egress. The one-shot `keys` service generates the attestation key into a named volume and mounts only its public half into Ante; no private keys are committed. Ante creates its own development signing key in another volume.

The script waits for readiness, invites a member through the host outbox, waits for Ante's token, validates and stages it through the proxy, signs in the stub identity, completes the attested exchange, submits user setup and verifies both acceptance and provisioning confirmation in the host inbox. It prints eight passing steps or the first failure with the host's received event types. A health check alone is not this verification.

For a manual walkthrough, create an invitation through the proxy:

```bash
curl --fail-with-body http://localhost:8080/host/invitations \
  -H 'Content-Type: application/json' \
  -d '{"email":"invitee@example.com","tenant":"Acme","roles":["Member"]}'
curl --fail-with-body http://localhost:8080/host/invitations/INVITATION_ID
```

Replace `INVITATION_ID` with the returned id and poll until `tokenIssued` is true. Open the returned `link` in a browser and sign in with the same email. The lobby UI requires the SPA to be built (`yarn build` in `Source/Ante`, with a valid PrimeUI license) before publishing, or a released image selected with `ANTE_IMAGE=ghcr.io/cratis/ante:<tag>`. Use `docker compose -f Fixture/compose.yml up -d --wait --pull never --no-build` after preparing that image online. The API script needs no SPA or license.

Set `ANTE_FIXTURE_PORT` before preparation/start to choose another loopback port; the script uses the same variable, or accepts `--base-url http://localhost:PORT`. Changing it after starting requires recreating the services. The fixture uses the same development-mode Ante environment keys as `cratis run`, planned in [RunStackPlanner.Ante.cs](https://github.com/Cratis/cli/blob/main/Source/Cli/Commands/Run/RunStackPlanner.Ante.cs), with fixture-specific host store, namespace and provider values.

The replacement seam for a real identity provider/proxy is the `proxy` and `keys` services and the stub sign-in. Replace them with AuthProxy and real authentication while preserving the attestation contract and updating Ante's public trust key, issuer, audience and provider pins to match. No account or client schema is assumed here. This is the path for [`cratis stage run`](https://github.com/Cratis/cli/issues/555) and the [offline suite](https://github.com/Cratis/Stage/issues/988), not an implementation of either.

Remove all fixture containers and persisted invitation/key state when finished:

```bash
docker compose -f Fixture/compose.yml down -v
```

## Run Ante as a Screenplay application's onboarding service

`cratis run` starts one Ante for a Screenplay application with no `Ante:*` configuration and no secrets. With MongoDB and Chronicle running, the equivalent by hand is:

```bash
cd Source/Ante
Ante__Onboarding__Enabled=true \
Ante__Onboarding__Development__Enabled=true \
Ante__Onboarding__HostStore=Application \
Ante__Onboarding__PublicBaseUrl=http://localhost:5002 \
ASPNETCORE_ENVIRONMENT=Development \
dotnet run --urls http://localhost:5002
```

Ante generates a signing key into `Source/Ante/.ante-development/` on first start and reuses it afterwards; the folder is ignored by Git. Add `Ante__Onboarding__Legal__Folder=<folder>` to present `terms.md` and `privacy.md` from a folder, and `Ante__Onboarding__Personas__0=Owner` to restrict grantable personas. Development mode is refused when the environment name is production-like, in Kubernetes, or when a configured URL is not loopback. See [Configuration](./configuration.md#onboarding-contract-screenplay-applications).

## Run a development server

With MongoDB and Chronicle independently configured:

1. From `Source/Ante`, run `dotnet run --urls http://localhost:5002`. The checked-in launch profile otherwise declares port 5050.
2. From `Source/Ante` in another terminal, run `yarn dev`. Vite serves the SPA on port 9002 and forwards `/api`, `/.cratis` and `/scalar` to 5002.
3. If you need Development OpenAPI, change Vite's `/openapi` proxy target from **5000** to your backend port. Check `/healthz/ready` for MongoDB connectivity; it does not check Chronicle.

Running the SPA without a host, invite token and proxy is not an end-to-end test. The backend's Development configuration includes `http://{tenant}.localhost:8090/` as a sample host destination, not a provisioned host. For a deployed network and trust boundary see [Deployment](./deployment.md).
