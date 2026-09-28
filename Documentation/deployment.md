---
title: Deploy Ante
description: Prepare a secure Ante instance, connect dependencies, and check the handoff without overstating readiness.
---

:::danger[Do not expose Ante directly]
`/_invite/exchange` checks JWT shape, GUID `jti`, and expiry, but does **not** validate signature, issuer or audience. Ante must only be reachable through a verifying proxy that controls forwarded identity claims and headers. Restrict generated `/api` routes as well: some queries expose pending invitations without owner checks. See [Security and trust](./security.md), [trust protocol #11](https://github.com/Cratis/Ante/issues/11), [ownership #13](https://github.com/Cratis/Ante/issues/13), and [API exposure #58](https://github.com/Cratis/Ante/issues/58).
:::

## Prepare the dependencies

Deploy Chronicle server **19.13.1** alongside the pinned `Cratis.Chronicle` client **19.13.1** (`Directory.Packages.props`) and make MongoDB reachable from Ante. The client performs a wire-contract compatibility check on connect and logs the result. Matching 19.13.1 is the target; server **19.4.7** was also verified with this client (the whole end-to-end suite passes against it), so the server does not have to be upgraded first. See [Upgrade an existing deployment](#upgrade-an-existing-deployment). `Program.cs` passes `Ante:EventStore` and a fixed `Ante:Namespace` to Arc/Chronicle. Direct's deployment supplies `Cratis__Chronicle__ConnectionString` (`Direct/Deployment/AnteDeployment.cs`); Ante does not set a local server URL.

MongoDB is needed during startup: `Program.cs` awaits creation of accepted-invitation indexes before wiring the HTTP pipeline. Routing validation throws `AnteEventStoreNotConfigured`, `AnteNamespaceNotConfigured`, or `AnteHostStoresInvalid` for an empty local store/namespace, an empty/duplicate/self source, or conflicting old/new source settings. Ante waits up to 30 seconds for Chronicle artifact registration, then registers one source-specific inbox reactor and filtered subscription for each configured host store; startup fails if registration does not succeed. The observation stream can still fail after registration. Ante does **not** validate the RSA signing key at startup; an empty/malformed key may fail later in `InvitationTokenIssuingReactor`.

Provide a host publishing invitations to an outbox store in `Ante:HostStores` (default `["Direct"]`), a verifying proxy, host observers of Ante's outbox, and a real redirect URL. The checked-in base `HostAppUrl` is an unusable example, `https://{tenant}.example.com/`, especially for join invitations, which do not replace `{tenant}`. There is no bundled compose, Helm, or standalone host/proxy fixture.

## Prepare the image and secrets

The published image is `ghcr.io/cratis/ante:<version>`; `latest` follows non-prerelease releases only. The publish workflow runs on selected `main` source/build-path pushes or manual dispatch; its image and NuGet jobs run only if the release action decides to publish. CI builds the frontend and publishes `Source/Ante/out` before Docker builds `Source/Ante/Dockerfile`. Docker expects that prebuilt directory in its `Source/Ante` context; it does not build the app. The Dockerfile uses the .NET 10 ASP.NET Noble base image, declares port 8080 and starts `dotnet Ante.dll`. The ASP.NET base image normally configures HTTP port 8080; set `ASPNETCORE_HTTP_PORTS=8080` explicitly in your deployment and verify the listener. Terminate TLS at the trusted proxy. The image removes `appsettings.Development.json`.

Generate a distinct RSA keypair for a non-development environment:

```bash
openssl genrsa -out ante-private.pem 2048
openssl rsa -in ante-private.pem -pubout -out ante-public.pem
```

Inject the private PEM, including line breaks, as `Ante__Invitations__Token__PrivateKeyPem` from a protected secret source. Supply the public key to your **external verifier**; `Ante__Invitations__Token__PublicKeyPem` is not required for Ante to sign. Do not reuse the checked-in Development key. Key changes require a verifier overlap plan, or outstanding links may become unusable.

This **illustrative** `docker run` assumes Chronicle and MongoDB already exist on a private container network, the proxy alone can reach port 8080, and a secret manager has exported the full PEM as `Ante__Invitations__Token__PrivateKeyPem` without printing it. Replace the placeholders, including the Chronicle connection string, for your infrastructure; it is not a standalone first-invitation fixture:

```bash
docker run --name ante --network <private-network> \
  -e ASPNETCORE_HTTP_PORTS=8080 \
  -e Cratis__Chronicle__ConnectionString='<chronicle-connection-string>' \
  -e Cratis__MongoDB__Server='mongodb://<mongo-host>:27017' \
  -e Cratis__MongoDB__Database=Ante \
  -e Ante__EventStore=Ante \
  -e Ante__Namespace=Default \
  -e Ante__HostStores__0=Direct \
  -e Ante__HostAppUrl='https://<real-host>/' \
  -e Ante__Invitations__Token__PrivateKeyPem \
  ghcr.io/cratis/ante:<version>
```

The `-e` flag without a value forwards that variable from the invoking environment; do not type the PEM into a shell command or commit it. Configure `IdentityProviders__Providers__0__Name` and `__Issuer` for the proxy providers where attribution needs them. Protect optional host backchannels, transport and query-string logs. See [Configuration](./configuration.md) for settings and [Host integration](./host-integration.md) for the required proxy behavior.

## Verify before routing traffic

1. Confirm at the proxy that invalid signatures, spoofed forwarded identity headers and direct Ante requests are rejected. This must include generated `/api` queries, not only `/_invite/exchange`.
2. Use `GET /healthz` as a **liveness** probe; it runs zero checks. Use `GET /healthz/ready` as **readiness**; it checks MongoDB and whether each configured inbox reactor is subscribed and active (or replaying), with a three-second per-check bound (200 healthy, 503 unhealthy). The response is a status word, without connection diagnostics. Subscribed and active is not proof that cross-store delivery has caught up, or that token issuance, proxy and host provisioning are working.
3. Observe a GUID-sourced invitation from host outbox through Ante inbox to `InvitationTokenIssued` in Ante outbox. Then check an acceptance and any legal fact in the outbox before calling the handoff healthy; verify provisioning independently in the host. See [Diagnose onboarding](./operations.md) for stalled stages.

## Upgrade an existing deployment

These steps upgrade an Ante built on the 19.4.7 client, which received invitations through the typed inbox reactor, to this release. They were rehearsed against one kernel with its MongoDB data kept across the change, in both orders: server 19.4.7 → new Ante → server 19.13.1, and server 19.4.7 → server 19.13.1 (old Ante still running) → new Ante. The old Ante connects to a 19.13.1 server (`Compatibility check passed - client 19.4.7 ... against server 19.13.1`), and the new Ante connects to a 19.4.7 server, so either order works.

1. Stop the old Ante before starting the new one. The two must not consume `inbox-Direct` at the same time.
2. Start the new Ante with `Ante:HostStores` unset (Direct). It re-registers the Direct inbox under the old reactor id `Ante.Invitations.Receiving.IncomingInvitationReactor` and subscription `Direct` and continues from the old position: invitations the old Ante received are not received again, no second token is issued, and only invitations arriving after the switch get an `InvitationInboxEventRecorded` marker.
3. **Restart Ante once more**, then check that the observers `JoinTenantAcceptanceOutbox`, `OrganizationSetupOutbox`, `OrganizationRegistrationOutbox` and `LegalTermsAcceptanceOutbox` are active. Builds before this release pinned these reactors with `[EventLog]`, which Chronicle's client also registers as a projection under the same id. In every rehearsal run those builds **did not forward** acceptances, legal acceptance, organization setup or registrations to the outbox: the projection took the observer, and when the reactor won instead (after a server restart) its activation failed with `No command context is available`. On its first start the new Ante no longer registers the projections; the kernel retires them and in doing so disconnects the reactors that now own those ids. After the second start they are active.
4. Expect the backlog: once active, the forwarding reactors start from the beginning of the event log and publish every acceptance, legal acceptance, organization setup and registration recorded since the affected builds. Hosts receive these late and must handle them idempotently, as they already must for redelivery.

`/healthz/ready` covers the inbox reactors only; it does not report the forwarding reactors of step 3. The rehearsal used the `-development` images with their default kernel settings.

## Cut over or roll back routing

For another lobby, give it its own Ante instance, destination store/namespace and MongoDB state as appropriate; for example `Ante__EventStore=DirectLobby`, `Ante__Namespace=Default`, `Ante__HostStores__0=Direct`. For two Studio producers use `Ante__HostStores__0=Studio` and `Ante__HostStores__1=StudioAdmin`. These source stores must forward in Ante's configured namespace; there is no automatic mapping. Before a live rename, rehearse observer registration, check each source inbox and destination history, and route host observers to the destination before switching Ante. Removing a source is a drain/subscription-ownership migration, not an automatic unsubscribe or history deletion. Changing `Ante:EventStore`, `Ante:Namespace` or `Ante:HostStores` does not migrate events. Rollback selects the earlier routing; reconcile facts written during cutover rather than assuming they were copied back. Custom builds with a legacy non-Direct reactor cursor need an explicit cursor mapping plan.

Development alone exposes `/openapi/...`; unmatched paths under reserved `/api`, `/openapi`, `/_invite` and `/healthz` return 404 instead of the SPA page.
