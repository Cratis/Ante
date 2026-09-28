---
title: Contracts
description: Public event payloads, event-source-id rules, and Ante's explicit HTTP boundary.
---

`Source/Ante.Contracts` publishes nine current `[EventType]` records. `InvitationTokenIssued` is generation 2; its generation-1 record remains available for migration. The other records, including `InvitationRejected`, use the implicit/default generation. The checked-in `Source/Ante.Contracts/EventSchemas.snapshot.json` guards the camelCase Chronicle wire schema (the naming policy configured in `Program.cs`) for each event type and generation. `Source/Ante.Contracts/ReleasedEventSchemas.json` marks the published generations; the snapshot guard permits edits to unreleased generations but never to released ones. At each release, add every newly published generation to that marker after verifying its snapshot, then commit the marker so future edits require a new generation and migration. Contract types use the assembly's `[EventStore("Ante")]` annotation for default observer routing; an instance with a different `Ante:EventStore` needs explicit routing in host observers. See [Host integration](./host-integration.md).

## Event-source-id rules

For an invited journey, the host mints a **nonempty GUID** invitation id and uses its lowercase, hyphenated `D` representation as the Chronicle event source id on the invitation and any revocation. Ante checks this at the inbox boundary. An invitation with a noncanonical id (including uppercase, braced, unhyphenated, empty-GUID or non-GUID values) produces `InvitationRejected(InvalidInvitationId)` in Ante's outbox under the original id, without creating a pending invitation or issuing a token; a revocation with such an id is ignored. After a canonical id has been revoked or accepted (in either flow), a different host invitation event under that id produces `InvitationRejected(InvitationIdReused)` instead of a new receipt or token. Ante checks the local event log, not the pending-invitation projection, and records the originating inbox sequence number with each new receipt. A reactor redelivery of that same inbox event (same inbox sequence number) is a no-op, not a rejection. For receipts predating the marker, Ante compares the receipt's persisted reactor causation (`eventSequenceId` and `eventSequenceNumber`) with the current `inbox-Direct` delivery, including pending invitations. Repeated historical receipts for one delivery do not reserve later inbox numbers. A later event with the same payload is still reuse if it has no earlier receipt. Chronicle's outbox-to-inbox forwarder can copy a host event a second time under a *new* inbox sequence number: after revocation or acceptance, that copy can surface as `InvitationIdReused`. Hosts should ignore a rejection for an id they already closed. Reuse is only rejected after revocation or acceptance; a different event under a still-pending id creates another receipt and token. Hosts must still use a new id for every distinct invite. The rejection warning omits the id. If an outbox append fails, `OutboxPublicationFailed` can include the untrusted id in its diagnostic message; restrict access to those logs. Ante reuses that id on the token and acceptance outbox events and as JWT `jti`. It is event context, **not a field in the payload**. Self-service registration uses a client-generated registration GUID as its event source id; there is no earlier invitation. Correlation id is different metadata. Host consumers must tolerate repeated delivery.

## Host outbox to Ante inbox

| Event (namespace `Ante.Contracts.Invitations`) | Payload fields (C# types) |
| --- | --- |
| `UserInvitedToJoinTenant` | `Email: Email`; `TenantName: TenantName`; `Roles: IReadOnlyList<RoleName>` |
| `UserInvitedToCreateTenant` | `Email: Email`; `Roles: IReadOnlyList<RoleName>` |
| `InvitationRevoked` | None; reuse the invitation event source id |

## Ante outbox to host inbox

| Event | Namespace | Payload fields (C# types) |
| --- | --- | --- |
| `InvitationTokenIssued` (generation 2) | `Ante.Contracts.Invitations` | `FlowType: InvitationFlowType`; `Token: string`; `ExpiresAt: DateTimeOffset` (UTC JWT `exp`) |
| `InvitationRejected` (generation 1) | `Ante.Contracts.Invitations` | `Reason: InvitationRejectionReason` (`InvalidInvitationId = 0`, `InvitationIdReused = 1`) |
| `InvitationToJoinTenantAccepted` | `Ante.Contracts.Invitations` | `TenantName: TenantName`; `IdentityProvider: IdentityProviderName`; `IdentityProviderSubject: string`; `FirstName: FirstName`; `MiddleName: MiddleName`; `LastName: LastName`; `Email: Email`; `Roles: IReadOnlyList<RoleName>` |
| `InvitationToCreateTenantAccepted` | `Ante.Contracts.Invitations` | `TenantName: TenantName`; `IdentityProvider: IdentityProviderName`; `IdentityProviderSubject: string`; `FirstName: FirstName`; `MiddleName: MiddleName`; `LastName: LastName`; `Email: Email`; `Roles: IReadOnlyList<RoleName>` |
| `OrganizationRegistrationCompleted` | `Ante.Contracts.Organization` | `TenantName: TenantName`; `Subject: string`; `IdentityProvider: IdentityProviderName`; `FirstName: FirstName`; `MiddleName: MiddleName`; `LastName: LastName`; `Email: Email` |
| `LegalTermsAccepted` | `Ante.Contracts.Legal` | `TenantName: TenantName`; `IdentityProvider: IdentityProviderName`; `IdentityProviderSubject: string`; `Version: LegalVersion` |

`InvitationFlowType` is `JoinTenant = 0` or `CreateTenant = 1`. The other named types are concepts from `Ante.Contracts.Invitations` (and `LegalVersion` from `Ante.Contracts.Legal`); do not treat a nullable command `MiddleName` as nullable in the event. Invited acceptance `Email` comes from the host invitation's pending read model, not the sign-in's email. Self-service `OrganizationRegistrationCompleted.Email` is resolved from forwarded sign-in claims or the identity-name header; it can be empty if none resembles an address. `LegalTermsAccepted` is present only if a legal source is registered in Ante and documents were accepted. The four locally recorded acceptance/registration/legal facts, token issuance and invitation rejections all check the outbox append result before the observer advances. The original correlation id, occurrence and compliance subject are preserved. Existing generation-1 `InvitationTokenIssued` facts upcast in the event log with `ExpiresAt = DateTimeOffset.UnixEpoch`; inbox/outbox rows are not covered by Chronicle 19.4.7's migration job, so deserialization of a missing `expiresAt` also supplies that sentinel. The actual historical expiry is unknown, and the sentinel must never be treated as a valid future expiry. Within Ante's own store, Chronicle downcasts to the generation-1 schema when needed. Across stores, hosts receive generation-2 payloads and must ignore unknown members such as `expiresAt` until they adopt the new contract; Chronicle does not downcast at that boundary.

## HTTP surface

| Caller → receiver | Route | Request / response and limits |
| --- | --- | --- |
| Trusted proxy → Ante | `POST /_invite/exchange` | `Authorization: Bearer <invitation JWT>` plus camelCase JSON `subject: string`, `identityProvider: string`, `providerKey?: string`, `issuer?: string`. Returns 200 on stored session, 400 for malformed JSON, missing/blank subject, unresolved provider, or a missing/invalid/expired token; a MongoDB failure can return 500. Ante verifies RS256 against its signing public key plus the optional additional public key, and checks configured issuer/audience (fixed in [Ante #51](https://github.com/Cratis/Ante/issues/51)). |
| Browser → Ante | `/invite/{token}` or `?token=...` | SPA URL used to select the invitation flow; not an authenticated API result. Cratis AuthProxy serves `/invite/{token}` itself and redirects to the lobby. Until [Ante #68](https://github.com/Cratis/Ante/issues/68) ships, the same path requested from Ante *directly* returns 404 because the dots in the JWT make the SPA fallback treat it as a file; `/?token=...` works there. |
| Browser → Ante | `/register` or `/register/*` | Self-service SPA route. |
| Probe → Ante | `GET /healthz`, `GET /healthz/ready` | Liveness without dependency checks; readiness checks MongoDB only (200 healthy, 503 unhealthy). |
| Developer → Ante | `/openapi/...` | Development-only generated OpenAPI endpoints; not served outside Development. |
| Ante → host | `GET {IdentityBackchannelUrl}/in-use?organization={tenantName}&subject={identityProviderSubject}` | JSON `{ "isInUse": boolean }`; optional join-invitation preflight, skipped if URL, organization or subject is empty. Not authoritative uniqueness. |
| Ante → host | `GET {HostOutcomeUrl}/outcome?attempt={invitationGuid}` | JSON `{ "status": "pending" | "succeeded" | "failed", "reasonCode"?: string }`; optional and informational. Ante only calls the host if `IsVerifiedOwnerOf(attemptId)` succeeds; otherwise it returns `Unknown` without calling the host. |

The two host GETs use URL-escaped parameters and no application credential in their own client code: restrict transport and endpoint access in deployment. HTTP/network, cancellation, unsupported content and JSON parse failures degrade to `false` for identity preflight or `Unknown` for outcome; other failures can propagate. The host outcome endpoint does not apply to self-registration.

## Generated routes in this checkout

The committed TypeScript proxies enumerate 14 generated routes, including the controller proxy for exchange. Command proxies submit commands, query proxies read state; query behavior and parameters come from their generated proxy types. The routes are not an authorization policy: see [Security and trust](./security.md#generated-api-exposure). Regenerate proxies after a Debug build and inspect Development OpenAPI for the paths in the version you deploy.

| Proxy / operation | Route |
| --- | --- |
| `HostUrl` | `/api/configuration/host-url` |
| `SignInPathFor` | `/api/configuration/sign-in-path-for` |
| `GetConfiguration` | `/api/configuration/get-configuration` |
| `Exchange` | `/_invite/exchange` |
| `ForAttempt` | `/api/invitations/host-outcome/for-attempt` |
| `SetupOrganization` | `/api/invitations/organization-setup` |
| `StatusForInvitation` (organization; owner-checked) | `/api/invitations/organization-setup/status-for-invitation` |
| `StatusForRegistration` (self-service; owner-checked snapshot) | `/api/invitations/organization-setup/status-for-registration` |
| `PendingCreateOrganizationForCurrentInvitee` | `/api/invitations/receiving/pending-create-organization-for-current-invitee` |
| `PendingJoinForCurrentInvitee` | `/api/invitations/receiving/pending-join-for-current-invitee` |
| `AcceptInvitation` | `/api/invitations/user-setup` |
| `StatusForInvitation` (join; owner-checked) | `/api/invitations/user-setup/status-for-invitation` |
| `Current` (legal documents) | `/api/legal/current` |
| `RegisterOrganization` | `/api/organization/registration` |

`RegistrationOwnerRecorded` is local to Ante, carries the self-service owner's subject and provider for status authorization, and is **not** part of `Ante.Contracts` or forwarded to the host. `OnboardingAttemptClaimed` is also local: an append-time per-source constraint prevents reusing an id across join acceptance, invited organization setup, and self-service registration. Session-based invitation ownership matches both subject and resolved provider; forwarded `jti` still relies on the proxy. Pre-existing registrations without this event return the same Pending/empty-name response as unknown ids. `Program.cs` sets `RoutePrefix = "api"`, omits command names, and skips one segment. Unmatched paths under `/api`, `/openapi`, `/_invite`, and `/healthz` return 404, not the SPA shell.
