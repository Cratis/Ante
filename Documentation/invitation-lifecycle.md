---
title: Invitation lifecycle
description: How invitations become tokens, local acceptance records, published facts, and host handoffs.
---

The invitation id ties together the host's event, Ante's receipt, the JWT `jti`, and the published outcome. It is the Chronicle event source id, not a payload field or the event correlation id.

## From host event to link

The host appends `UserInvitedToJoinTenant` or `UserInvitedToCreateTenant` to its outbox with a GUID invitation id. Ante's cross-store inbox reactor records a local receipt event and creates a pending read model. A second reactor issues an RS256 token and writes `InvitationTokenIssued` directly to Ante's outbox. The host then builds `/invite/{token}` on the lobby's public URL and distributes that link. Ante does not send mail. The token reactor parses the event source id as a GUID; a non-GUID id fails issuance. See [Contracts](./contracts.md) for the exact events.

The browser reads `invite_type` only to choose a screen. Exchange records a subject/provider session in MongoDB after parsing the token; the boundary and its limitations are in [Security and trust](./security.md).

## From command to publication

`AcceptInvitation` joins an existing tenant; `SetupOrganization` creates one from an invitation; `RegisterOrganization` starts without an invitation. The first two require a matching invitation owner and pending invitation. `OneUseJoinTenantInvitation` and `OneUseCreateTenantInvitation` constrain each invitation's acceptance at append time, even when a stale projection still shows pending. Both organization-creating commands share an append-time `UniqueOrganizationNameConstraint` plus an earlier read-model check. Organization names must be nonempty, at most 50 characters, and contain no space, null, `/`, `\\`, `.`, `"`, `$`, `*`, `<`, `>`, `:`, `|`, `?` or `+`. Personal first/last names are required, middle name optional; each supplied name is at most 100 characters and rejects control and text-direction-override characters. A successful command records acceptance or registration in Ante's local event log. When legal documents are configured in Ante's process and accepted, it also records `LegalTermsAccepted`. Outbox reactors forward these local facts, preserving payload plus correlation id, occurrence time and compliance subject. A failed append from these forwarding reactors raises `OutboxPublicationFailed`; delivery can still be repeated, so host consumers must be idempotent.

Status `Pending` means there is no local acceptance yet. `Recorded` means the local append committed but publication is not complete: **do not resubmit**. `Accepted` means the acceptance/registration fact and any required legal fact have reached Ante's outbox. It does not say the host has consumed or provisioned them. An optional host-outcome HTTP lookup can show a later host result for invitation-bound flows without changing Ante's published status. The frontend's 20-second wait timeout is not a publication failure. See [Operations](./operations.md) for diagnosing a stall.

## Revocation and consent

`InvitationRevoked` under the same event source id removes the pending invitation when its projection processes the local revocation receipt; a later command rejects the now-missing pending record. It does not revoke a token or an exchanged session at the exchange endpoint.

The default `NoLegalDocumentSource` presents no documents. A custom source registered **in Ante's process** can provide a terms/policy pair and version; the wizard asks for acceptance, and command execution rechecks the current version before recording a legal fact. A new version resets a visible consent choice when that version reaches the client. See [Customization](./customization.md) for the composition requirement.
