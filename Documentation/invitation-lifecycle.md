---
title: Invitation lifecycle
description: How invitations become tokens, local acceptance records, published facts, and host handoffs.
---

The invitation id ties together the host's event, Ante's receipt, the JWT `jti`, and the published outcome. It is the Chronicle event source id, not a payload field or the event correlation id.

## From host event to link

The host appends `UserInvitedToJoinTenant` or `UserInvitedToCreateTenant` to its outbox under a fresh, nonempty GUID invitation id in lowercase hyphenated `D` format. Ante rejects a noncanonical id with `InvitationRejected(InvalidInvitationId)`; if a previously revoked or accepted id is used for a different invitation, Ante emits `InvitationRejected(InvitationIdReused)` instead. Neither rejection creates a new receipt or token. Ante's cross-store inbox reactor records valid invitations locally and creates a pending read model. A second reactor issues an RS256 token and writes `InvitationTokenIssued` directly to Ante's outbox. The host then builds `/invite/{token}` on the proxy's public origin and distributes that link. Cratis AuthProxy validates the token's signature and lifetime on that path (issuer and audience when configured on both sides), runs sign-in and the exchange, and redirects to the lobby frontend. Ante does not send mail. See [Contracts](./contracts.md#event-source-id-rules) for the canonical-id and rejection rules.

The browser reads `invite_type` only to choose a screen; it is not authorization. Legacy exchange verifies the capability and stores a subject/provider session from the proxy's body. In Attested mode, newly issued links include the recipient `email` and lobby-scope `tenant_id`; the following staged sequence replaces the legacy exchange:

```mermaid
sequenceDiagram
    participant Host
    participant Proxy as AuthProxy
    participant Ante
    participant User
    Host->>Proxy: /invite/{recipient-bound capability}
    Proxy->>Ante: POST /_invite/stage (signed invite-stage assertion, transaction, token, challenge)
    Ante->>Ante: Verify capability and stage evidence; store expiring transaction
    Ante-->>Proxy: 200
    Proxy->>User: Authenticate with approved provider
    Proxy->>Ante: POST /_invite/exchange (signed invite-complete assertion, transaction only)
    Ante->>Ante: Verify actor, verified email, challenge and pending invitation
    Ante->>Ante: Atomically insert attested completion/session and assertion ID
    Ante-->>Proxy: 200
    Proxy->>User: Redirect to Ante lobby with canonical identity forwarded
    User->>Ante: Submit owner-checked acceptance command
    Ante->>Ante: Read invitation log; append under revocation concurrency scope
    Ante-->>Host: Publish acceptance and optional legal facts
```

A lost completion response can be retried by the same actor without extending the staged expiry. Another actor or expired transaction fails. A forwarded invitation `jti` only helps find the exact attested session; it cannot replace completion. Self-registration has no stage or invitation token: it records its own owner before submission. See [Security and trust](./security.md) for the two modes and [Contracts](./contracts.md#http-surface) for the wire shape.

## From command to publication

`AcceptInvitation` joins an existing tenant; `SetupOrganization` creates one from an invitation; `RegisterOrganization` starts without an invitation. The first two require a matching invitation owner and pending invitation. In Attested mode that owner must have an exact live actor-bound completion, not just a forwarded `jti`. `OneUseJoinTenantInvitation` and `OneUseCreateTenantInvitation` constrain each invitation's acceptance at append time, even when a stale projection still shows pending. `OnboardingAttemptClaimed` also prevents reusing an id across these flows and self-service registration. Both organization-creating commands share an append-time `UniqueOrganizationNameConstraint` plus an earlier read-model check. Organization names must be nonempty, at most 50 characters, and contain no space, null, `/`, `\`, `.`, `"`, `$`, `*`, `<`, `>`, `:`, `|`, `?` or `+`. Personal first/last names are required, middle name optional; each supplied name is at most 100 characters and rejects control and text-direction-override characters. A successful command records acceptance or registration in Ante's local event log. When legal documents are configured in Ante's process and accepted, it also records `LegalTermsAccepted`. Outbox reactors forward these local facts, preserving payload plus correlation id, occurrence time and compliance subject. A failed append from these forwarding reactors raises `OutboxPublicationFailed`; delivery can still be repeated, so host consumers must be idempotent.

Status `Pending` means there is no local acceptance yet. `Recorded` means the local append committed but publication is not complete: **do not resubmit**. `Accepted` means the acceptance/registration fact and any required legal fact have reached Ante's outbox. It does not say the host has consumed or provisioned them. An optional host-outcome HTTP lookup can show a later host result for invitation-bound flows without changing Ante's published status. The frontend's 20-second wait timeout is not a publication failure. See [Operations](./operations.md) for diagnosing a stall.

## Revocation and consent

`InvitationRevoked` under the same event source id records a local revocation and removes the pending invitation when the projection catches up. Attested stage and new completion check the local event log, as do join and create acceptance. Their append-time concurrency scope includes revocation, so a revocation appended between the check and acceptance defeats the whole acceptance batch. A revocation still in the host outbox has not yet reached Ante and cannot fence a local acceptance. Legacy exchange does not revoke its token/session merely because a later revocation was received; committed attested retries do not restore revoked invitation authority.

The default `NoLegalDocumentSource` presents no documents. A custom source registered **in Ante's process** can provide a terms/policy pair and version; the wizard asks for acceptance, and command execution rechecks the current version before recording a legal fact. A new version resets a visible consent choice when that version reaches the client. See [Customization](./customization.md) for the composition requirement.
