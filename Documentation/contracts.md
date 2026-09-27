---
title: Contracts
description: Public event payloads, event-source-id rules, and Ante's explicit HTTP boundary.
---

These are the eight `[EventType]` records in `Source/Ante.Contracts`. All use the attribute's implicit/default generation (no explicit event-type id or generation in these declarations). Contract types use the assembly's `[EventStore("Ante")]` annotation for default observer routing; an instance with a different `Ante:EventStore` needs explicit routing in host observers. See [Host integration](./host-integration.md).

## Event-source-id rules

For an invited journey, the host mints a **GUID** invitation id and uses its string representation as the Chronicle event source id on the invitation and any revocation. Ante's token reactor calls `Guid.Parse(context.EventSourceId.Value)`; a non-GUID id fails before a token can be issued. Ante reuses that id on the token and acceptance outbox events and as JWT `jti`. It is event context, **not a field in the payload**. Self-service registration uses a client-generated registration GUID as its event source id; there is no earlier invitation. Correlation id is different metadata. Host consumers must tolerate repeated delivery.

## Host outbox to Ante inbox

| Event (namespace `Ante.Contracts.Invitations`) | Payload fields (C# types) |
| --- | --- |
| `UserInvitedToJoinTenant` | `Email: Email`; `TenantName: TenantName`; `Roles: IReadOnlyList<RoleName>` |
| `UserInvitedToCreateTenant` | `Email: Email`; `Roles: IReadOnlyList<RoleName>` |
| `InvitationRevoked` | None; reuse the invitation event source id |

## Ante outbox to host inbox

| Event | Namespace | Payload fields (C# types) |
| --- | --- | --- |
| `InvitationTokenIssued` | `Ante.Contracts.Invitations` | `FlowType: InvitationFlowType`; `Token: string` |
| `InvitationToJoinTenantAccepted` | `Ante.Contracts.Invitations` | `TenantName: TenantName`; `IdentityProvider: IdentityProviderName`; `IdentityProviderSubject: string`; `FirstName: FirstName`; `MiddleName: MiddleName`; `LastName: LastName`; `Email: Email`; `Roles: IReadOnlyList<RoleName>` |
| `InvitationToCreateTenantAccepted` | `Ante.Contracts.Invitations` | `TenantName: TenantName`; `IdentityProvider: IdentityProviderName`; `IdentityProviderSubject: string`; `FirstName: FirstName`; `MiddleName: MiddleName`; `LastName: LastName`; `Email: Email`; `Roles: IReadOnlyList<RoleName>` |
| `OrganizationRegistrationCompleted` | `Ante.Contracts.Organization` | `TenantName: TenantName`; `Subject: string`; `IdentityProvider: IdentityProviderName`; `FirstName: FirstName`; `MiddleName: MiddleName`; `LastName: LastName`; `Email: Email` |
| `LegalTermsAccepted` | `Ante.Contracts.Legal` | `TenantName: TenantName`; `IdentityProvider: IdentityProviderName`; `IdentityProviderSubject: string`; `Version: LegalVersion` |

`InvitationFlowType` is `JoinTenant = 0` or `CreateTenant = 1`. The other named types are concepts from `Ante.Contracts.Invitations` (and `LegalVersion` from `Ante.Contracts.Legal`); do not treat a nullable command `MiddleName` as nullable in the event. Invited acceptance `Email` comes from the host invitation's pending read model, not the sign-in's email. Self-service `OrganizationRegistrationCompleted.Email` is resolved from forwarded sign-in claims or the identity-name header; it can be empty if none resembles an address. `LegalTermsAccepted` is present only if a legal source is registered in Ante and documents were accepted. The four locally recorded acceptance/registration/legal facts are forwarded with their correlation id, occurrence and compliance subject; token issuance appends directly and does not use that forwarding helper.

## HTTP surface

| Caller → receiver | Route | Request / response and limits |
| --- | --- | --- |
| Trusted proxy → Ante | `POST /_invite/exchange` | `Authorization: Bearer <invitation JWT>` plus camelCase JSON `subject: string`, `identityProvider: string`, `providerKey?: string`, `issuer?: string`. Returns 200 on stored session, 400 for missing/malformed/expired token or invalid body; a MongoDB failure can return 500. **No signature/issuer/audience verification inside Ante.** |
| Browser → Ante | `/invite/{token}` or `?token=...` | SPA URL used to select the invitation flow; not an authenticated API result. |
| Browser → Ante | `/register` or `/register/*` | Self-service SPA route. |
| Probe → Ante | `GET /healthz`, `GET /healthz/ready` | Liveness without dependency checks; readiness checks MongoDB only (200 healthy, 503 unhealthy). |
| Developer → Ante | `/openapi/...` | Development-only generated OpenAPI endpoints; not served outside Development. |
| Ante → host | `GET {IdentityBackchannelUrl}/in-use?organization={tenantName}&subject={identityProviderSubject}` | JSON `{ "isInUse": boolean }`; optional join-invitation preflight, skipped if URL, organization or subject is empty. Not authoritative uniqueness. |
| Ante → host | `GET {HostOutcomeUrl}/outcome?attempt={invitationGuid}` | JSON `{ "status": "pending" | "succeeded" | "failed", "reasonCode"?: string }`; optional and informational. Ante only calls the host if `IsVerifiedOwnerOf(attemptId)` succeeds; otherwise it returns `Unknown` without calling the host. |

The two host GETs use URL-escaped parameters and no application credential in their own client code: restrict transport and endpoint access in deployment. HTTP/network, cancellation, unsupported content and JSON parse failures degrade to `false` for identity preflight or `Unknown` for outcome; other failures can propagate. The host outcome endpoint does not apply to self-registration.

## Generated routes in this checkout

The committed TypeScript proxies enumerate 13 generated routes, including the controller proxy for exchange. Command proxies submit commands, query proxies read state; query behavior and parameters come from their generated proxy types. The routes are not an authorization policy: see [Security and trust](./security.md#generated-api-exposure). Regenerate proxies after a Debug build and inspect Development OpenAPI for the paths in the version you deploy.

| Proxy / operation | Route |
| --- | --- |
| `HostUrl` | `/api/configuration/host-url` |
| `SignInPathFor` | `/api/configuration/sign-in-path-for` |
| `GetConfiguration` | `/api/configuration/get-configuration` |
| `Exchange` | `/_invite/exchange` |
| `ForAttempt` | `/api/invitations/host-outcome/for-attempt` |
| `SetupOrganization` | `/api/invitations/organization-setup` |
| `StatusForInvitation` (organization) | `/api/invitations/organization-setup/status-for-invitation` |
| `AllPendingInvitationsToCreateOrganization` | `/api/invitations/receiving/all-pending-invitations-to-create-organization` |
| `AllPendingInvitationsToJoin` | `/api/invitations/receiving/all-pending-invitations-to-join` |
| `AcceptInvitation` | `/api/invitations/user-setup` |
| `StatusForInvitation` (join) | `/api/invitations/user-setup/status-for-invitation` |
| `Current` (legal documents) | `/api/legal/current` |
| `RegisterOrganization` | `/api/organization/registration` |

`Program.cs` sets `RoutePrefix = "api"`, omits command names, and skips one segment. Unmatched paths under `/api`, `/openapi`, `/_invite`, and `/healthz` return 404, not the SPA shell.
