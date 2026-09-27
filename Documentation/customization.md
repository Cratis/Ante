---
title: Customize the lobby
description: Supply branding and legal documents, and understand the single-locale limit.
---

Ante has URL-based branding and an in-process legal-document extension. There is no deployed theme catalog, legal authoring UI or runtime locale setting.

## Set branding assets

1. Host a logo and stylesheet under URLs your lobby browser can reach, or mount static assets into Ante's `wwwroot` and use same-origin paths. Set `Ante:LogoUrl` and `Ante:CustomCssUrl` to those URLs. Empty values use the wordmark and default CSS.
2. Load the lobby through your actual ingress and check the browser's asset responses and Content-Security-Policy. The CSS loader accepts same-origin HTTP(S) or cross-origin **HTTPS**; it rejects malformed URLs, other schemes and cross-origin plain HTTP. A failed CSS load removes the link; a failed logo load displays the wordmark.
3. Check keyboard focus, contrast and layout after applying custom CSS. The URL check does **not** sanitize CSS; the operator controls trusted assets and any CSP `style-src` restrictions.

## Supply legal documents

1. If you own a custom Ante composition, implement `Ante.Legal.ILegalDocumentSource.GetCurrent()` returning `Task<LegalDocumentSet?>`. Its result has `TermsAndConditions: LegalDocumentBody`, `PrivacyPolicy: LegalDocumentBody` and `Version: LegalVersion`; return `null` when nothing is offered. Register it in **Ante's process** before serving traffic. Stock `Program.cs` installs `NoLegalDocumentSource`; registering an implementation in a separately deployed host does nothing.
2. Verify the current documents appear as a conditional terms step in all three wizards. On submission Ante validates acceptance against the current version and emits `LegalTermsAccepted` only when documents were actually configured and accepted. Update content and version together; a stale submission is rejected. This repository does not contain an admin UI or a runtime configuration option for legal text.

## Add a language in source

Only English (`en`, rendered as `en-US`) ships. There is no deployment-time locale selection. A source change needs a reviewed translation in `Source/Ante/Locales/`, an entry in `Source/Ante/Locale/Locale.ts` and its resolution path, plus localized backend validation: `Program.cs` currently forces invariant culture. Run frontend and backend checks from [Local development](./local-development.md) before claiming another locale is supported. Legal document language remains the custom document source's responsibility.
