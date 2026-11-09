---
title: Lobby journeys
description: How the join, invited-creation, and self-registration wizards select a flow and recover from publication delay.
---

Ante offers three journeys in one SPA. `/register` and its subpaths select self-service registration; other SPA paths use identity details first, then the token's **unverified** `invite_type` for display, and finally default to the join wizard. The token may come from `/invite/{token}` or `?token=...`; the browser also uses its unverified `jti` as the invitation id it submits and polls, while the server checks ownership. Screen selection is not authorization; see [Security and trust](./security.md).

## Join an existing tenant

The visitor enters their first, optional middle and last name in `UserSetupPage`, then accepts terms if documents are configured in Ante's process.

`AcceptInvitation` checks invitation ownership and the pending invitation, and optionally asks the host whether the subject is already associated with a user in that organization. Once Ante's acceptance and optional legal fact are **published to its outbox**, the wizard can hand off to the host. It does not wait for host provisioning.

## Create a tenant from an invitation

In `OrganizationSetupPage`, the visitor supplies an organization name, personal names and optional legal consent.

`SetupOrganization` checks ownership and pending invitation; organization-name uniqueness has both a read-model check and an append-time constraint shared with self-registration. The published event asks the host to provision the organization and user; it is not proof the host did so.

## Register without an invitation

At `/register`, the visitor supplies the same organization and name fields in `RegistrationPage`, which submits through `RegisterOrganization`. When `Ante:Registration:Enabled` is `false`, the page shows only a "sign-up is not available" notice (with the optional `ClosedUrl` link) and the server rejects `BeginRegistration` and `RegisterOrganization`. Host-authored copy from `Ante:Registration:Content:{locale}` (title, offer text, highlights, pricing and log-in links, completion message) is rendered as plain text around the wizard.

While `Ante:Registration:Review` holds the sign-up, the page shows that it is waiting for review (and, after a decline, that it was not approved) instead of the waiting spinner; the status query reports `Held` or `Declined`.

Query-string keys listed in `Ante:Registration:ContextKeys` are captured from the registration link, kept for the tab, and submitted as signup context. The tab keeps its registration id in `sessionStorage`, not the form's personal data. If storage works, a reload in that tab can resume checking the same operation. Storage may be unavailable, and another tab or session need not share that pointer.

The form does not collect or verify an email address; the published registration email may be empty or an unverified address-shaped sign-in claim. A host requiring a verified contact must handle that during provisioning, not treat the wizard's completion as mailbox verification.

With `Ante:HostOutcomeUrl` configured, registration shows the same completion screen as the invited journeys once published; the host outcome is answered only to the sign-in recorded as the registration's owner.

## Waiting and handoff

The pages read durable status: `pending` (no local acceptance), `recorded` (committed locally; **do not submit again**) and `accepted` (primary fact and any legal fact published to Ante's outbox).

The 20-second client timeout only changes the waiting UI, not the underlying event status; a later accepted result can still complete the journey.

With `Ante:HostOutcomes:Mode=Disabled` and no `Ante:HostOutcomeUrl`, completion redirects to `HostAppUrl` (substituting `{tenant}` **only** in organization journeys, with a provider sign-in path when resolved). The join wizard uses the literal configured host URL; the checked-in `{tenant}.example.com` value must be replaced for join handoff. With a host outcome configured, success permits handoff; failure or exhausted waiting shows the completion screen with **Check again**, not **Continue**. In `DurableRequired` mode only a verified durable success permits automatic or manual handoff, regardless of the advisory outcome. A failed lookup or a response without an explicit mode keeps handoff blocked and is retried; a timeout offers another check without changing the durable status. A malformed host URL surfaces a handoff error after publication rather than undoing the submission. See [Operations](./operations.md) for stalls.
