---
title: Ownership and limits
description: What Ante owns, what the host owns, and which integration gaps remain.
---

Ante owns signing invitation tokens, the onboarding UI and its local acceptance and outbox publication. A host owns who to invite, how to distribute the link, membership, role grants, tenant provisioning, email delivery and any legal document content. The host can use a copy-link workflow instead of email. Ante's `Accepted` status means publication to its outbox, not successful host provisioning. An optional host-outcome GET can display later host status for invited journeys, but its result is informational and Continue remains available.

## Deployment boundary

One Ante instance uses one configured Chronicle store and fixed namespace; it does not select a namespace per request. Its **incoming** host store is compiled as `Direct`, so a different host store requires a rebuild, even though Ante's own store is configurable. The host must also supply a proxy that enforces the [Security and trust](./security.md) boundary and controls access to generated API queries. These are current limits, not security properties supplied by Ante. See [Architecture](./architecture.md).

## Optional surfaces

Stock Ante has no legal-document source beyond `NoLegalDocumentSource`; to collect consent, a custom source must be registered in Ante's own process. Branding accepts a logo URL and a CSS URL, without presets or CSS sanitization. The UI supports English only, and backend validation uses invariant culture. An outer React error boundary handles caught render errors with a recovery view that does not display the exception text; it cannot promise recovery from failures before rendering or from every network error. See [Customization](./customization.md) and [Wizards](./wizards.md).

Ante does not supply a standalone host/proxy test fixture, deployment manifests, automatic signing-key rotation, or a publication-lag readiness metric. None of these missing facilities should be mistaken for a deliberate host responsibility without a product decision.
