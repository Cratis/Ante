---
title: Work on Ante locally
description: Run the repository's exact backend and frontend checks and understand local runtime prerequisites.
---

The repository does not include a runnable Chronicle + MongoDB + host + verifying-proxy fixture. You can build and test the source without claiming a complete invitation journey; to exercise one, provision those services and use [Host integration](./host-integration.md). The committed Development RSA keypair is throwaway and must never be reused outside local development.

## Check backend and frontend

CI uses .NET 10 and Node 22. From the repository root, run its exact backend commands:

```bash
dotnet build Ante.slnx --configuration Debug
dotnet build Ante.slnx --configuration Release -p:CratisProxiesOutputPath=
dotnet test Ante.slnx --configuration Debug --no-build --logger "console;verbosity=minimal"
```

The Debug build regenerates TypeScript proxies and compiles colocated backend specifications. The Release build deliberately disables a second proxy generation. To match CI's frontend job, from the repository root run:

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

## Run a development server

With MongoDB and Chronicle independently configured:

1. From `Source/Ante`, run `dotnet run --urls http://localhost:5002`. The checked-in launch profile otherwise declares port 5050.
2. From `Source/Ante` in another terminal, run `yarn dev`. Vite serves the SPA on port 9002 and forwards `/api`, `/.cratis` and `/scalar` to 5002.
3. If you need Development OpenAPI, change Vite's `/openapi` proxy target from **5000** to your backend port. Check `/healthz/ready` for MongoDB connectivity; it does not check Chronicle.

Running the SPA without a host, invite token and proxy is not an end-to-end test. The backend's Development configuration includes `http://{tenant}.localhost:8090/` as a sample host destination, not a provisioned host. For a deployed network and trust boundary see [Deployment](./deployment.md).
