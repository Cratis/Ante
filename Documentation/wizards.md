---
title: Lobby journeys
description: How the join, invited-creation, and self-registration wizards select a flow and recover from publication delay.
---

Ante offers three journeys in one SPA. `/register` and its subpaths select self-service registration; other SPA paths use identity details first, then the token's **unverified** `invite_type` for display, and finally default to the join wizard. The token may come from `/invite/{token}` or `?token=...`. Screen selection is not authorization; see [Security and trust](./security.md).

## Join an existing tenant

`UserSetupPage` takes first, optional middle and last name, then terms acceptance if documents are configured in Ante's process. `AcceptInvitation` checks invitation ownership and the pending invitation, and optionally asks the host whether the subject is already associated with a user in that organization. Once Ante's acceptance and optional legal fact are **published to its outbox**, the wizard can hand off to the host. It does not wait for host provisioning.

## Create a tenant from an invitation

`OrganizationSetupPage` asks for organization name, personal names and optional legal consent. `SetupOrganization` checks ownership and pending invitation; organization-name uniqueness has both a read-model check and an append-time constraint shared with self-registration. The published event asks the host to provision the organization and user; it is not proof the host did so.

## Register without an invitation

`RegistrationPage` at `/register` gathers the same organization and name fields through `RegisterOrganization`. Its browser `sessionStorage` pointer holds a per-tab registration id, not the form's personal data. If storage works, a reload in that tab can resume checking the same operation. Storage may be unavailable, and another tab or session need not share that pointer. Registration has no invitation-owner session and cannot use the optional host-outcome view.

## Waiting and handoff

The pages read durable status: `pending` (no local acceptance), `recorded` (committed locally; **do not submit again**) and `accepted` (primary fact and any legal fact published to Ante's outbox). The 20-second client timeout only changes the waiting UI, not the underlying event status; a later accepted result can still complete the journey. With no `Ante:HostOutcomeUrl`, completion redirects to `HostAppUrl` (substituting `{tenant}` **only** in organization journeys, with a provider sign-in path when resolved). The join wizard uses the literal configured host URL; the checked-in `{tenant}.example.com` value must be replaced for join handoff. With that option, the two invited journeys show a completion screen with an informational host outcome and a **Continue** action; they do not automatically redirect. Self-registration still redirects. A malformed host URL surfaces a handoff error after publication rather than undoing the submission. See [Operations](./operations.md) for stalls.
