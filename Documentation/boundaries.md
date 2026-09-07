---
title: Boundaries
description: What Ante deliberately does not do, and why — so integrators know what remains their own responsibility.
---

Ante's scope is narrow on purpose: signed invitations, a lobby, and event-based coordination. Everything below is a deliberate exclusion, not a gap waiting to be filled.

## Sending email

Ante mints tokens and appends `InvitationTokenIssued` — the host builds the actual invitation link from the token and sends it, by whatever email system it already uses. Ante has no email-sending dependency at all.

## Legal document authoring and versioning

There is no admin UI for writing terms and conditions, no revision history, and no bundled fallback text. `ILegalDocumentSource` is Ante's one extension point for this: a host that wants the wizards to collect consent supplies its own implementation, sourced from wherever it already manages that content. See [Invitation Lifecycle](./invitation-lifecycle.md#legal-consent).

## Provisioning

Seats, trials, billing, tenant databases — Ante's job ends the moment it appends an accepted or registered event to its own outbox. The host reacts to that event to actually provision anything. There is consequently no "waiting for provisioning" UI state built into the wizards beyond the acceptance-status poll described in [Wizards](./wizards.md#status-polling-recovery-and-redirect); the source material this was extracted from had a multi-minute wait/retry state machine for exactly that concern, which does not apply here because there is nothing external for the lobby itself to wait for.

## Admin invite-authoring UI

Deciding *who* to invite, with what role, into which tenant, is a host concern. Ante only reacts to the invitation events a host already decided to append.

## Multi-tenancy of Ante itself

Ante runs single-tenant, in Chronicle's `Default` namespace. A product needing several isolated lobbies runs several Ante instances, differentiated by `Ante:EventStore` (see [Configuration](./configuration.md#example-a-named-lobby-instance)) — not by adding tenancy inside Ante.

## Host membership and grants

Ante resolves *who is onboarding* and hands the host a correlated accepted/registered event with the invitee's name, email, and identity-provider subject. What roles or grants that translates to inside the host's own authorization model is entirely the host's decision.

## Branding presets

`Ante:LogoUrl` and `Ante:CustomCssUrl` let a deployment point at its own logo and stylesheet, but Ante ships no built-in theme catalog or design-system integration beyond that. A host wanting a fully branded lobby supplies its own assets through those two settings.

## Next steps

- [Host Integration](./host-integration.md) — what Ante does hand the host, precisely.
- [Index](./index.md) — the full map of this documentation set.
