---
title: Customize the lobby
description: Supply branding and legal documents, and understand the single-locale limit.
---

Ante has URL-based branding and two legal-document sources. There is no deployed theme catalog, legal authoring UI or runtime locale setting.

## Set branding assets

1. Host a logo and stylesheet under URLs your lobby browser can reach, or mount static assets into Ante's `wwwroot` and use same-origin paths. Set `Ante:LogoUrl` and `Ante:CustomCssUrl` to those URLs. Empty values use the wordmark and default CSS.
2. Load the lobby through your actual ingress and check the browser's asset responses and Content-Security-Policy. The CSS loader accepts same-origin HTTP(S) or cross-origin **HTTPS**; it rejects malformed URLs, other schemes and cross-origin plain HTTP. A failed CSS load removes the link; a failed logo load displays the wordmark.
3. Check keyboard focus, contrast and layout after applying custom CSS. The URL check does **not** sanitize CSS; the operator controls trusted assets and any CSP `style-src` restrictions.

## Supply legal documents

Choose one source for the deployment:

- **InProcess (default):** If you own a custom Ante composition, implement `Ante.Legal.ILegalDocumentSource.GetCurrent()` returning `Task<LegalDocumentSet?>`. Its result has `TermsAndConditions: LegalDocumentBody`, `PrivacyPolicy: LegalDocumentBody` and `Version: LegalVersion`; return `null` when nothing is offered. Register it in **Ante's process** before serving traffic. Stock `Program.cs` installs `NoLegalDocumentSource`; registering an implementation in a separately deployed host does nothing. InProcess `null` means no legal step.
- **Inbox:** Set `Ante:Legal:Source=Inbox`, `Ante:Legal:PublisherStore` to an entry in `Ante:HostStores`, and `Ante:Legal:DocumentSetId` to a stable non-GUID event-source id. The publisher writes a full `LegalDocumentSetPublished` snapshot to that store's **outbox** under that id. Ante receives and activates it on the local legal stream, then publishes `LegalDocumentSetActivated` to its outbox. Start Ante in Inbox mode and confirm the publisher store's outbox-to-Ante subscription includes `LegalDocumentSetPublished` **before** the host publishes the set. In Chronicle 19.13.1, adding this type to an existing subscription does not backfill earlier outbox events of that type; if no set is active, republish the current set after confirming the filter. An identical republish is a harmless no-op if it was already activated. Install the host's reverse acknowledgement subscription as well. Before the first activation, the legal query reports `IsUnavailable=true` and all three onboarding submissions are blocked; there is no in-process or bundled fallback. Use the same namespace on host and Ante.

Verify the current documents appear as a terms step in all three wizards. On submission Ante validates acceptance against the current activated version and emits `LegalTermsAccepted` only when documents were actually presented and accepted. Change content and version together; a stale submission is rejected. In Inbox mode, each publication is a complete immutable pair with a strictly increasing positive revision; the host keeps the version-to-text archive. An older delivery never rolls back the current set, identical duplicates do nothing, and contradictory reuse is recorded locally as `LegalDocumentSetRejected`. The newest *activated* revision is current, not the newest host draft or an in-flight publication. See [Host integration](./host-integration.md) and [Operations](./operations.md) for delivery and diagnosis.

## Add a language in source

Only English (`en`, rendered as `en-US`) ships. There is no deployment-time locale selection. A source change needs a reviewed translation in `Source/Ante/Locales/`, an entry in `Source/Ante/Locale/Locale.ts` and its resolution path, plus localized backend validation: `Program.cs` currently forces invariant culture. Run frontend and backend checks from [Local development](./local-development.md) before claiming another locale is supported. Legal document language remains the custom document source's responsibility.
