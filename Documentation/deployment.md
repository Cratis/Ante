---
title: Deploy Ante
description: Prepare a secure Ante instance, connect dependencies, and check the handoff without overstating readiness.
---

:::danger[Do not expose Ante directly]
Legacy `/_invite/exchange` verifies the invitation capability (fixed in [Ante #51](https://github.com/Cratis/Ante/issues/51)); Attested exchange verifies signed AuthProxy stage/completion assertions and the recipient-bound capability. Ante must still only be reachable through a trusted proxy that controls forwarded identity claims and headers. A forwarded `jti` is an ownership shortcut only in Legacy mode, not in Attested mode. Restrict generated `/api` routes as well, even though pending-invitation and status queries now check ownership. See [Security and trust](./security.md), [trust protocol #11](https://github.com/Cratis/Ante/issues/11), and [ownership #13](https://github.com/Cratis/Ante/issues/13).
:::

## Prepare the dependencies

Deploy Chronicle server **19.13.1** alongside the pinned `Cratis.Chronicle` client **19.13.1** (`Directory.Packages.props`) and make MongoDB reachable from Ante. The client performs a wire-contract compatibility check on connect and logs the result. Matching 19.13.1 is the target; server **19.4.7** was also verified with this client (the whole end-to-end suite passes against it), so the server does not have to be upgraded first. See [Upgrade an existing deployment](#upgrade-an-existing-deployment). `Program.cs` passes `Ante:EventStore` and a fixed `Ante:Namespace` to Arc/Chronicle. Direct's deployment supplies `Cratis__Chronicle__ConnectionString` (`Direct/Deployment/AnteDeployment.cs`); Ante does not set a local server URL.

MongoDB is needed during startup: `Program.cs` awaits creation of accepted-invitation indexes before wiring the HTTP pipeline. Routing validation throws `AnteEventStoreNotConfigured`, `AnteNamespaceNotConfigured`, or `AnteHostStoresInvalid` for an empty local store/namespace, an empty/duplicate/self source, or conflicting old/new source settings. In the background, Ante waits up to 30 seconds per attempt for Chronicle artifact registration, then registers one source-specific inbox reactor and filtered subscription for each configured host store. Registration failure does not stop the process: it stays live while `/healthz/ready` returns 503, and retries with capped exponential backoff (up to 30 seconds), logging "Host inbox registration failed; retrying in the background" on each failure. Routing configuration-validation failures still stop startup. The observation stream can still fail after registration. Invitation token startup validation rejects a configured private key that cannot parse as RSA; an invalid optional `PublicKeyPem` also fails startup with `InvitationTokenConfigurationInvalid`. An empty private key is allowed - a deployment that only offers self-service registration needs none - and startup warns, `/healthz/ready` reports the `invitation-signing` check as degraded (still 200), and invitations wait until a key is configured; every exchange is rejected. Empty `Ante:Invitations:Token:Issuer` or `Audience` settings derive per-deployment values, logged at startup. The checked-in base key is a placeholder, not a deployable production configuration.

Provide a host publishing invitations to an outbox store in `Ante:HostStores` (default `["Direct"]`), a verifying proxy, host observers of Ante's outbox, and a real redirect URL. The checked-in base `HostAppUrl` is an unusable example, `https://{tenant}.example.com/`, especially for join invitations, which do not replace `{tenant}`. [`Deployment/kubernetes/ante.yaml`](https://github.com/Cratis/Ante/blob/main/Deployment/kubernetes/ante.yaml) is a reference Kubernetes deployment (ConfigMap with every production decision, signing-key Secret, Deployment with liveness `/healthz` and readiness `/healthz/ready` probes, and Service); [`Deployment/README.md`](https://github.com/Cratis/Ante/blob/main/Deployment/README.md) has the fronting-proxy checklist and the version compatibility table. Translate it to your own tooling; the environment variable names are the contract. There is no bundled compose, Helm chart, or standalone host/proxy fixture.

## Prepare the image and secrets

The published image is `ghcr.io/cratis/ante:<version>`; `latest` follows non-prerelease releases only. The publish workflow runs on selected `main` source/build-path pushes or manual dispatch; its image and NuGet jobs run only if the release action decides to publish. CI builds the frontend and publishes `Source/Ante/out` before Docker builds `Source/Ante/Dockerfile`. Docker expects that prebuilt directory in its `Source/Ante` context; it does not build the app. The Dockerfile uses the .NET 10 ASP.NET Noble base image, declares port 8080 and starts `dotnet Ante.dll`. The ASP.NET base image normally configures HTTP port 8080; set `ASPNETCORE_HTTP_PORTS=8080` explicitly in your deployment and verify the listener. Terminate TLS at the trusted proxy. The image removes `appsettings.Development.json`.

Generate a distinct RSA keypair for a non-development environment:

```bash
openssl genrsa -out ante-private.pem 2048
openssl rsa -in ante-private.pem -pubout -out ante-public.pem
```

For a deployment that issues or exchanges invitations, inject the private PEM, including line breaks, as `Ante__Invitations__Token__PrivateKeyPem` from a protected secret source. Every token carries this deployment's issuer and audience: set `Ante__Invitations__Token__Issuer` and `Ante__Invitations__Token__Audience` to the values your fronting proxy expects, or leave them empty to derive `urn:cratis:ante:{EventStore}:{Namespace}` (and `...:lobby`). The effective values are logged at startup. Ante derives its current trusted verification key from the private key; distribute the matching public key to the external verifier. `Ante__Invitations__Token__PublicKeyPem` and `Ante__Invitations__Token__PreviousPublicKeyPems__N` add trusted exchange-verification keys (for example, the outgoing keys during rotation), not signing keys. Tokens name their signing key in `kid`, so rotation never invalidates outstanding links while the old key stays trusted. Do not reuse the checked-in Development key. Coordinate an overlap with external verifiers and allow outstanding links to expire before removing the old key.

**Before upgrading:** `PublicKeyPem` was previously ignored by Ante at exchange and is now trusted to verify invitation signatures. Remove any non-Ante key from this setting before upgrading; a host or proxy key left there would grant that party invitation-signing authority.

**Upgrading to 1.0:** no draining. Tokens issued before the upgrade carry no issuer or audience; a deployment that derives its issuer and audience keeps accepting those - if signed by a trusted key, carrying `iat`, and issued no later than 15 minutes after it first started on 1.0 (the grace covers a rolling upgrade) - until that start plus the grace plus one `Ante:Invitations:Token:Expiry`, then stops by itself. Tokens carrying only one of `iss` and `aud` are refused, and the signature must come from a trusted key. A deployment that sets `Issuer` or `Audience` explicitly never accepts tokens without them. See [Signing options](./configuration.md#signing-options).

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
  -e Ante__Invitations__Token__Issuer='https://<ante-issuer>/' \
  -e Ante__Invitations__Token__Audience='<ante-lobby-audience>' \
  ghcr.io/cratis/ante:<version>
```

The `-e` flag without a value forwards that variable from the invoking environment; do not type the PEM into a shell command or commit it. Configure `IdentityProviders__Providers__0__Name` and `__Issuer` for the proxy providers where attribution needs them. Protect optional host backchannels, transport and query-string logs. See [Configuration](./configuration.md) for settings and [Host integration](./host-integration.md) for the required proxy behavior.

## Verify before routing traffic

1. Confirm Ante rejects invalid-signature, wrong-issuer/audience and expired tokens at exchange. Confirm at the proxy that spoofed forwarded identity headers and direct Ante requests are rejected. This must include generated `/api` queries, not only `/_invite/exchange`.
2. Use `GET /healthz` as a **liveness** probe; it runs zero checks. Use `GET /healthz/ready` as **readiness**; it checks MongoDB and whether each configured inbox reactor is subscribed and active (or replaying), with a three-second per-check bound (200 healthy, 503 unhealthy). The response is a status word, without connection diagnostics. Subscribed and active is not proof that cross-store delivery has caught up, or that token issuance, proxy and host provisioning are working.
3. Observe a GUID-sourced invitation from host outbox through Ante inbox to `InvitationTokenIssued` in Ante outbox. Then check an acceptance and any legal fact in the outbox before calling the handoff healthy; verify provisioning independently in the host. See [Diagnose onboarding](./operations.md) for stalled stages.

## Switch an invitation proxy to Attested mode

**Before upgrading Ante in either exchange mode**, deploy host consumers that register and handle `InvitationTokenIssued` generation 3, then verify a real token reaches each host inbox and remains a usable JWT. Ante emits generation 3 even in Legacy mode. An isolated generation-2-only host read a usable generation-3 token across stores on Chronicle 19.13.1 in the integration suite, but its raw MongoDB inbox stored the bearer JWT in plaintext because it lacked generation-3 PII metadata. That observation is not a supported consumer-upgrade substitute or a promise about other server versions. Do not upgrade the producer first.

1. Upgrade host token consumers for `InvitationTokenIssued` generation 3 before the producer changes. Pin Ante's capability-signing public key in AuthProxy and separately provision AuthProxy's attestation signing key plus the matching **public** key pins in Ante. Configure Ante's token issuer/audience/key and attestation issuer/audience/lobby scope/provider/assurance policy; confirm the proxy's `tenant_id` resolution equals that scope. For Direct, plan deployment changes and verified-email/assurance mapping for its providers. For Studio, plan replacing local token signing and adapting contract/store routing first; it does not consume Ante today.
2. Stop issuing Legacy links and let every outstanding Legacy link expire under the **actual configured** `Ante:Invitations:Token:Expiry`, or keep an isolated Legacy deployment for existing links only. If no drain is possible, revoke and reissue those invitations as new ids with new links after cutover. Switching an AuthProxy instance to signed stage/completion does not make old email-less, scope-less links work there. Never route signed-required links to a Legacy Ante deployment, and never let failed attestation fall through to Legacy.
3. Update proxy `/api` allowlists to permit `POST /api/organization/registration/start` (`BeginRegistration`) before routing the new lobby. This route records the self-registration owner before the wizard submits; blocking it breaks registration even if `/api/organization/registration` is permitted. On the isolated new path configure AuthProxy `Invite:StageUrl`, `ExchangeUrl`, `TenantClaim=tenant_id`, `EmailClaim=email`, `MatchingTenantInvitationDestination=Lobby` and its `Invite:Attestation` signing keys; set Ante `Exchange:Mode=Attested` and pin those public keys. Verify the complete [host configuration](./host-integration.md#enable-staged-attested-invitation-exchange). Startup must create the stage and session indexes before traffic. First check both flows: stage, login, completion, owner-checked pending/status, acceptance and outbox delivery; reject wrong actor, replay, expired transaction and concurrent locally recorded revocation. This repository has no live AuthProxy interoperability test, so run one before declaring rollout ready.
4. For rollback, stop signing new links and route only still-valid **Legacy** links to the isolated Legacy deployment. Do not send newly issued Attested links there. Keep the Attested deployment and its MongoDB state available for already completed sessions or explicitly reissue invitations as new ids; changing `Mode` does not convert sessions or tokens. Restore proxy routing only after verifying the target mode matches every link that reaches it.

This is an opt-in **minor** upgrade: old event identities and explicitly configured Legacy exchange remain supported. It is not an assurance-preserving rollback; never silently downgrade an actor whose new session was attested. See [Security and trust](./security.md#choose-an-exchange-mode).

## Upgrade from a release without working outbox forwarding

Releases v0.5.0 through v0.10.0 never forwarded acceptances, legal acceptance, organization setup or self-registration to the outbox ([Ante #72](https://github.com/Cratis/Ante/issues/72)). Releases v0.4.x and earlier forwarded them when the store was named `Ante`, and need no second restart. The forwarding reactors had been registered as empty projections under the same observer ids. A previous rehearsal against one kernel with MongoDB data kept across the change tested both server 19.4.7 → new Ante → server 19.13.1 and server 19.4.7 → server 19.13.1 (old Ante still running) → new Ante. The 19.4.7 client connects to a 19.13.1 server, and the 19.13.1 client connects to a 19.4.7 server.

1. Stop the old Ante before starting the new one; do not let both consume `inbox-Direct` at once. Start the new Ante with `Ante:HostStores` unset (Direct). It keeps the old reactor id `Ante.Invitations.Receiving.IncomingInvitationReactor` and subscription `Direct` and continues from the old cursor: previously received invitations are not received again, and only later invitations receive an `InvitationInboxEventRecorded` marker.
2. Restart Ante a second time. The kernel retires the stray projections on the first start, disconnecting the reactors that now own their observer ids. After the second start, check that `JoinTenantAcceptanceOutbox`, `OrganizationSetupOutbox`, `OrganizationRegistrationOutbox` and `LegalTermsAcceptanceOutbox` are active and advancing, with no projections under those ids. `/healthz/ready` only covers the inbox reactors, not these forwarding reactors.
3. Expect a backlog. The reactors publish every historical `InvitationToJoinTenantAccepted`, `InvitationToCreateTenantAccepted`, `OrganizationRegistrationCompleted` and `LegalTermsAccepted` from the start of Ante's event log, including facts an earlier release (v0.4.x or before) already forwarded. Tell your hosts before the upgrade. Hosts must consume these idempotently, and should decide how to treat acceptances older than the upgrade (for example, grant membership, or ask the user to be invited again).

The rehearsal used `-development` images with their default kernel settings.

## Upgrade from a release where self-registration status stayed pending

On releases before the fix for the registration owner projection, a self-registering user's status never left `Pending`. The owner fields were missing from the `OrganizationSetupProgress` read-model schema, so reads returned no owner, and the owner subject was stored unencrypted in that collection. On a deployment whose `Ante:EventStore` is not `Ante`, the setup read models (`OrganizationSetupProgress`, `UserSetupProgress`, `AcceptedOrganizationName`) also observed `inbox-Ante` instead of the event log and stayed empty, which weakened the organization-name uniqueness pre-check.

After upgrading, replay those three projections from the start of the event log so every document is rebuilt under the corrected schema and sequence. This also rewrites the previously plaintext owner subjects in encrypted form.

## Cut over or roll back routing

For another lobby, give it its own Ante instance, destination store/namespace and MongoDB state as appropriate; for example `Ante__EventStore=DirectLobby`, `Ante__Namespace=Default`, `Ante__HostStores__0=Direct`. For two Studio producers use `Ante__HostStores__0=Studio` and `Ante__HostStores__1=StudioAdmin`. These source stores must forward in Ante's configured namespace; there is no automatic mapping. Before a live rename, rehearse observer registration, check each source inbox and destination history, and route host observers to the destination before switching Ante. Removing a source is a drain/subscription-ownership migration, not an automatic unsubscribe or history deletion. Changing `Ante:EventStore`, `Ante:Namespace` or `Ante:HostStores` does not migrate events. Rollback selects the earlier routing; reconcile facts written during cutover rather than assuming they were copied back. Custom builds with a legacy non-Direct reactor cursor need an explicit cursor mapping plan.

Development alone exposes `/openapi/...`; unmatched paths under reserved `/api`, `/openapi`, `/_invite` and `/healthz` return 404 instead of the SPA page.
