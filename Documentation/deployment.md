---
title: Deploy Ante
description: Prepare a secure Ante instance, connect dependencies, and check the handoff without overstating readiness.
---

:::danger[Do not expose Ante directly]
`/_invite/exchange` verifies RS256 signatures, lifetime, GUID `jti`, and configured issuer/audience before recording a session. Ante must still only be reachable through a trusted proxy that controls forwarded identity claims and headers; a forwarded `jti` on later requests is trusted from that proxy, not revalidated by Ante. Restrict generated `/api` routes as well, even though pending-invitation and status queries now check ownership. See [Security and trust](./security.md), [trust protocol #11](https://github.com/Cratis/Ante/issues/11), and [ownership #13](https://github.com/Cratis/Ante/issues/13).
:::

## Prepare the dependencies

Run a Chronicle server compatible with the pinned `Cratis.Chronicle` client **19.4.7** (`Directory.Packages.props`) and MongoDB reachable from Ante. `Program.cs` passes `Ante:EventStore` and a fixed `Ante:Namespace` to the Arc/Chronicle builder but does not directly assign the connection string. Direct's real Ante deployment supplies `Cratis__Chronicle__ConnectionString` (`Direct/Deployment/AnteDeployment.cs`), which is the verified deployment key; this repository has no local Chronicle URL or server-version pin. Validate the connection string format and server compatibility in the environment you deploy.

MongoDB is needed during startup: `Program.cs` awaits creation of accepted-invitation indexes before wiring the HTTP pipeline. Routing validation throws `AnteEventStoreNotConfigured`, `AnteNamespaceNotConfigured`, or `InboxSourceStoreCannotBeReconfigured` for empty local store/namespace or a configured host store different from the compiled `Direct`. Invitation token startup validation requires a parseable RSA private key in every environment and nonempty `Ante:Invitations:Token:Issuer` and `Audience` outside Development; an invalid optional `PublicKeyPem` also fails startup with `InvitationTokenConfigurationInvalid`. The checked-in base settings are placeholders, not a deployable production configuration.

Provide a host publishing invitations to outbox store `Direct` (or rebuild Ante for another name), a verifying proxy, host observers of Ante's outbox, and a real redirect URL. The checked-in base `HostAppUrl` is an unusable example, `https://{tenant}.example.com/`, especially for join invitations, which do not replace `{tenant}`. There is no bundled compose, Helm, or standalone host/proxy fixture.

## Prepare the image and secrets

The published image is `ghcr.io/cratis/ante:<version>`; `latest` follows non-prerelease releases only. The publish workflow runs on selected `main` source/build-path pushes or manual dispatch; its image and NuGet jobs run only if the release action decides to publish. CI builds the frontend and publishes `Source/Ante/out` before Docker builds `Source/Ante/Dockerfile`. Docker expects that prebuilt directory in its `Source/Ante` context; it does not build the app. The Dockerfile uses the .NET 10 ASP.NET Noble base image, declares port 8080 and starts `dotnet Ante.dll`. The ASP.NET base image normally configures HTTP port 8080; set `ASPNETCORE_HTTP_PORTS=8080` explicitly in your deployment and verify the listener. Terminate TLS at the trusted proxy. The image removes `appsettings.Development.json`.

Generate a distinct RSA keypair for a non-development environment:

```bash
openssl genrsa -out ante-private.pem 2048
openssl rsa -in ante-private.pem -pubout -out ante-public.pem
```

Inject the private PEM, including line breaks, as `Ante__Invitations__Token__PrivateKeyPem` from a protected secret source. Set distinct, nonempty `Ante__Invitations__Token__Issuer` and `Ante__Invitations__Token__Audience` for this deployment; startup refuses to run without them outside Development. Ante derives its current trusted verification key from the private key; distribute the matching public key to the external verifier. `Ante__Invitations__Token__PublicKeyPem` adds a second trusted exchange-verification key (for example, the outgoing key during rotation), not a signing key. Do not reuse the checked-in Development key. Coordinate an overlap with external verifiers and allow outstanding links to expire before removing the old key.

**Before upgrading:** `PublicKeyPem` was previously ignored by Ante at exchange and is now trusted to verify invitation signatures. Remove any non-Ante key from this setting before upgrading; a host or proxy key left there would grant that party invitation-signing authority.

This **illustrative** `docker run` assumes Chronicle and MongoDB already exist on a private container network, the proxy alone can reach port 8080, and a secret manager has exported the full PEM as `Ante__Invitations__Token__PrivateKeyPem` without printing it. Replace the placeholders, including the Chronicle connection string, for your infrastructure; it is not a standalone first-invitation fixture:

```bash
docker run --name ante --network <private-network> \
  -e ASPNETCORE_HTTP_PORTS=8080 \
  -e Cratis__Chronicle__ConnectionString='<chronicle-connection-string>' \
  -e Cratis__MongoDB__Server='mongodb://<mongo-host>:27017' \
  -e Cratis__MongoDB__Database=Ante \
  -e Ante__EventStore=Ante \
  -e Ante__Namespace=Default \
  -e Ante__HostAppUrl='https://<real-host>/' \
  -e Ante__Invitations__Token__PrivateKeyPem \
  -e Ante__Invitations__Token__Issuer='https://<ante-issuer>/' \
  -e Ante__Invitations__Token__Audience='<ante-lobby-audience>' \
  ghcr.io/cratis/ante:<version>
```

The `-e` flag without a value forwards that variable from the invoking environment; do not type the PEM into a shell command or commit it. Configure `IdentityProviders__Providers__0__Name` and `__Issuer` for the proxy providers where attribution needs them. Protect optional host backchannels, transport and query-string logs. See [Configuration](./configuration.md) for settings and [Host integration](./host-integration.md) for the required proxy behavior.

## Verify before routing traffic

1. Confirm Ante rejects invalid-signature, wrong-issuer/audience and expired tokens at exchange. Confirm at the proxy that spoofed forwarded identity headers and direct Ante requests are rejected. This must include generated `/api` queries, not only `/_invite/exchange`.
2. Use `GET /healthz` as a **liveness** probe; it runs zero checks. Use `GET /healthz/ready` as **readiness**; it pings MongoDB with a three-second per-check bound (200 healthy, 503 unhealthy). The response is a status word, without connection diagnostics. Readiness says nothing about Chronicle, outbox forwarding, proxy, or host provisioning.
3. Observe a GUID-sourced invitation from host outbox through Ante inbox to `InvitationTokenIssued` in Ante outbox. Then check an acceptance and any legal fact in the outbox before calling the handoff healthy; verify provisioning independently in the host. See [Diagnose onboarding](./operations.md) for stalled stages.

## Cut over or roll back routing

For another lobby on the same infrastructure, give it its own Ante instance, store/namespace and MongoDB database as appropriate; for example set `Ante__EventStore=DirectLobby` and `Ante__Namespace=Default`. This renames **Ante's own** store, not the compiled incoming host store `Direct`. Before a live rename, rehearse observer registrations and read-model catch-up against the destination (which may already hold history); point host observers to the destination **before** switching Ante. Changing `Ante:EventStore` or `Ante:Namespace` does not migrate events. Rollback selects the earlier routing again; facts written to the destination during the cutover are not copied back. Plan their reconciliation, rather than assuming rollback retains them.

Development alone exposes `/openapi/...`; unmatched paths under reserved `/api`, `/openapi`, `/_invite` and `/healthz` return 404 instead of the SPA page.
