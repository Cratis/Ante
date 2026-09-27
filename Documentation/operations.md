---
title: Diagnose onboarding
description: Locate missing invitations, failed token issuance, unpublished acceptance, and host-side delays.
---

Use the invitation's GUID event source id to trace it across host outbox, Ante inbox, Ante local event log and Ante outbox. A correlation id is separate metadata. Do not replay a command merely because the UI has waited 20 seconds.

## No invitation or token

1. Check that the host wrote `UserInvitedToJoinTenant` or `UserInvitedToCreateTenant` to its **outbox**, not just its event log, under a GUID id. `IncomingInvitationReactor` observes the inbox for host store `Direct`; a different store name requires rebuilding `InboxSourceStore.Name`, not just changing `Ante:InboxSourceStore`.
2. Look for Ante's local `JoinTenantInvitationReceived` or `CreateTenantInvitationReceived` under the same id. If absent, inspect cross-store observer registration/connectivity and inbox processing in Chronicle. `/healthz/ready` does not cover this path.
3. If receipt exists but `InvitationTokenIssued` does not appear in Ante's outbox, check `InvitationTokenIssuingReactor`'s partition errors and `PrivateKeyPem`. It calls `Guid.Parse` on the event source id; a non-GUID id fails. The direct token outbox append currently does **not** inspect its returned `AppendResult`, so absence of a thrown error alone is not proof of publication. Do not claim a token was sent until you can see the outbox fact.

## Recorded but not published

Check `UserSetupAcceptanceStatusView.StatusForInvitation` for joins or `OrganizationSetupAcceptanceStatusView.StatusForInvitation` for invited creation and self-registration. If it reports `Recorded`, the acceptance/registration fact committed to Ante's **local log** but the host-facing outbox is not yet confirmed. Do **not** resubmit. Compare local and outbox records under that id: the status reaches `Accepted` only when the primary fact **and any locally recorded `LegalTermsAccepted`** have reached the outbox. Inspect `JoinTenantAcceptanceOutbox`, `OrganizationSetupOutbox`, or `OrganizationRegistrationOutbox` for the main fact and `LegalTermsAcceptanceOutbox` for consent. Look for their partition errors (`OutboxPublicationFailed` when `AppendResult.IsSuccess` is false), Chronicle connectivity and observer progress; repeated forwarding may duplicate a fact, so the host must deduplicate. No independent publication-lag readiness metric is implemented.

If status is `Pending`, check that the invitation was received and its pending projection caught up; do not treat a transient UI spinner as an invitation. If status is `Accepted` but the host did not provision, trace the host observer and host-side checks: Ante's accepted state reports outbox publication, **not** host success. With `Ante:HostOutcomeUrl`, the invited-flow completion screen can ask the host for an informational result, but `Unknown` can mean absent/unreachable/malformed response and does not change Ante's status. Self-service registration has no such outcome lookup.

## Dependency and identity failures

`/healthz` runs no dependency checks; `/healthz/ready` pings **MongoDB only** with a three-second health-check timeout. Check Chronicle and the host independently even if ready is green. At startup Ante creates Mongo indexes for accepted-invitation sessions; exchange depends on MongoDB. A 400 from `/_invite/exchange` can mean malformed/missing bearer token, malformed GUID `jti` or expired/missing `exp`; it does not establish the reason the proxy accepted or rejected a request. Inspect proxy verification and access controls as described in [Security and trust](./security.md). A failed optional `/in-use` HTTP call can allow join acceptance to proceed; the host's own uniqueness rule must hold. Review [Security](./security.md) before diagnosing untrusted identity headers in production.
