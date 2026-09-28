---
title: Deploy Ante
description: Prepare a secure Ante instance, connect dependencies, and check the handoff without overstating readiness.
---

:::danger[Do not expose Ante directly]
`/_invite/exchange` checks JWT shape, GUID `jti`, and expiry, but does **not** validate signature, issuer or audience ([Ante #51](https://github.com/Cratis/Ante/issues/51)). Ante must only be reachable through a verifying proxy that controls forwarded identity claims and headers. Restrict generated `/api` routes as well: some queries expose pending invitations without owner checks. See [Security and trust](./security.md), [trust protocol #11](https://github.com/Cratis/Ante/issues/11), [ownership #13](https://github.com/Cratis/Ante/issues/13), and [API exposure #58](https://github.com/Cratis/Ante/issues/58).
:::

## Prepare the dependencies

Run a Chronicle server compatible with the pinned `Cratis.Chronicle` client **19.4.7** (`Directory.Packages.props`) and MongoDB reachable from Ante. `Program.cs` passes `Ante:EventStore` and a fixed `Ante:Namespace` to the Arc/Chronicle builder but does not directly assign the connection string. Direct's real Ante deployment supplies `Cratis__Chronicle__ConnectionString` (`Direct/Deployment/AnteDeployment.cs`), which is the verified deployment key; this repository has no local Chronicle URL or server-version pin. Validate the connection string format and server compatibility in the environment you deploy.

MongoDB is needed during startup: `Program.cs` awaits creation of accepted-invitation indexes before wiring the HTTP pipeline. Routing validation throws `AnteEventStoreNotConfigured`, `AnteNamespaceNotConfigured`, or `InboxSourceStoreCannotBeReconfigured` for empty local store/namespace or a configured host store different from the compiled `Direct`. It does **not** validate the RSA signing key at startup; an empty/malformed key may fail later in `InvitationTokenIssuingReactor`.

Provide a host publishing invitations to outbox store `Direct` (or rebuild Ante for another name), a verifying proxy, host observers of Ante's outbox, and a real redirect URL. The checked-in base `HostAppUrl` is an unusable example, `https://{tenant}.example.com/`, especially for join invitations, which do not replace `{tenant}`. There is no bundled compose, Helm, or standalone host/proxy fixture.

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
  -e Ante__HostAppUrl='https://<real-host>/' \
  -e Ante__Invitations__Token__PrivateKeyPem \
  ghcr.io/cratis/ante:<version>
```

The `-e` flag without a value forwards that variable from the invoking environment; do not type the PEM into a shell command or commit it. Configure `IdentityProviders__Providers__0__Name` and `__Issuer` for the proxy providers where attribution needs them. Protect optional host backchannels, transport and query-string logs. See [Configuration](./configuration.md) for settings and [Host integration](./host-integration.md) for the required proxy behavior.

## Verify before routing traffic

1. Confirm at the proxy that invalid signatures, spoofed forwarded identity headers and direct Ante requests are rejected. This must include generated `/api` queries, not only `/_invite/exchange`.
2. Use `GET /healthz` as a **liveness** probe; it runs zero checks. Use `GET /healthz/ready` as **readiness**; it pings MongoDB with a three-second per-check bound (200 healthy, 503 unhealthy). The response is a status word, without connection diagnostics. Readiness says nothing about Chronicle, outbox forwarding, proxy, or host provisioning.
3. Observe a GUID-sourced invitation from host outbox through Ante inbox to `InvitationTokenIssued` in Ante outbox. Then check an acceptance and any legal fact in the outbox before calling the handoff healthy; verify provisioning independently in the host. See [Diagnose onboarding](./operations.md) for stalled stages.

## Upgrade from a release without working outbox forwarding

Releases v0.5.0 through v0.10.0 never forwarded acceptances, legal acceptance, organization setup or self-registration to the outbox ([Ante #72](https://github.com/Cratis/Ante/issues/72)). Releases v0.4.x and earlier forwarded them when the store was named `Ante`, and need no second restart. The forwarding reactors had been registered as empty projections under the same observer ids. After upgrading:

1. Start the new version once. The Chronicle kernel retires the stray projections on this start, which disconnects the reactors that now own those observer ids.
2. Restart Ante a second time. The forwarding reactors become active.
3. Expect a backlog. The reactors publish every historical `InvitationToJoinTenantAccepted`, `InvitationToCreateTenantAccepted`, `OrganizationRegistrationCompleted` and `LegalTermsAccepted` from the start of Ante's event log, including facts an earlier release (v0.4.x or before) already forwarded. Tell your hosts before the upgrade. Hosts must consume these idempotently, and should decide how to treat acceptances older than the upgrade (for example, grant membership, or ask the user to be invited again).

Check the observers in Chronicle after the second start: the forwarding reactors must be active and advancing, and no projection may exist under their ids.

## Cut over or roll back routing

For another lobby on the same infrastructure, give it its own Ante instance, store/namespace and MongoDB database as appropriate; for example set `Ante__EventStore=DirectLobby` and `Ante__Namespace=Default`. This renames **Ante's own** store, not the compiled incoming host store `Direct`. Before a live rename, rehearse observer registrations and read-model catch-up against the destination (which may already hold history); point host observers to the destination **before** switching Ante. Changing `Ante:EventStore` or `Ante:Namespace` does not migrate events. Rollback selects the earlier routing again; facts written to the destination during the cutover are not copied back. Plan their reconciliation, rather than assuming rollback retains them.

Development alone exposes `/openapi/...`; unmatched paths under reserved `/api`, `/openapi`, `/_invite` and `/healthz` return 404 instead of the SPA page.
