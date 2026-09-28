---
title: Customize the lobby
description: Supply branding and legal documents, and configure English or Norwegian Bokmål.
---

Ante has URL-based branding, an in-process legal-document extension, and two bundled languages. There is no deployed theme catalog or legal authoring UI.

## Set branding assets

1. Host a logo and stylesheet under URLs your lobby browser can reach, or mount static assets into Ante's `wwwroot` and use same-origin paths. Set `Ante:LogoUrl` and `Ante:CustomCssUrl` to those URLs. Empty values use the wordmark and default CSS.
2. Load the lobby through your actual ingress and check the browser's asset responses and Content-Security-Policy. The CSS loader accepts same-origin HTTP(S) or cross-origin **HTTPS**; it rejects malformed URLs, other schemes and cross-origin plain HTTP. A failed CSS load removes the link; a failed logo load displays the wordmark.
3. Check keyboard focus, contrast and layout after applying custom CSS. The URL check does **not** sanitize CSS; the operator controls trusted assets and any CSP `style-src` restrictions.

## Supply legal documents

1. If you own a custom Ante composition, implement `Ante.Legal.ILegalDocumentSource.GetCurrent()` returning `Task<LegalDocumentSet?>`. Its result has `TermsAndConditions: LegalDocumentBody`, `PrivacyPolicy: LegalDocumentBody` and `Version: LegalVersion`; return `null` when nothing is offered. Register it in **Ante's process** before serving traffic. Stock `Program.cs` installs `NoLegalDocumentSource`; registering an implementation in a separately deployed host does nothing.
2. Verify the current documents appear as a conditional terms step in all three wizards. On submission Ante validates acceptance against the current version and emits `LegalTermsAccepted` only when documents were actually configured and accepted. Update content and version together; a stale submission is rejected. This repository does not contain an admin UI or a runtime configuration option for legal text.

## Choose the lobby language

English (`en`, rendered as `en-US`) and Norwegian Bokmål (`nb-NO`) ship with matching frontend and server messages. `nb` and `nb-NO` select Bokmål; `no` is accepted as a Bokmål compatibility alias, **not** Nynorsk (`nn`). Set `Ante:DefaultLocale` and `Ante:SupportedLocales` to choose the deployment default and limit what visitors can select; see [Configuration](./configuration.md#ante-runtime-options). Only shipped locales are accepted, and the default must be in the allowlist.

The visitor's choice in Display preferences persists in their browser when storage is available and takes precedence over a `?lang=nb-NO` link hint. Without browser storage, the choice lasts for the current page without a reload. Otherwise Ante picks the first allowed language in the browser's ordered language list, then the configured default, then English. Unsupported hints are ignored. Ante fetches its locale policy before rendering and sends the resolved language in `Accept-Language` on Arc HTTP requests. It also writes a same-origin `ante-locale` cookie (`SameSite=Lax`) before Arc connects: browser WebSocket/EventSource transports cannot set custom headers. The server uses `Accept-Language` for Arc HTTP requests, even when another tab has written a conflicting cookie. The cookie is only preferred for WebSocket and EventSource connections. Numeric parsing and identity comparisons remain invariant; only display messages change.

If you also serve sign-in or invitation email through AuthProxy, supply matching proxy-owned translations: Ante does not localize those pages or emails, nor can it guarantee a query hint survives an external sign-in redirect. Browser language still works on return. Legal text and its revision remain entirely host-owned; Ante never translates or substitutes it.

## Add a language in source

A new locale needs a translation with exactly the English keys under `Source/Ante/Locales/`, entries in `Source/Ante/Locale/Locale.ts` and negotiation, matching server resources under `Source/Ante/Resources/`, and the parity/spec checks. Chronicle freezes constraint messages when definitions are registered, outside a request; Ante maps known constraint names to localized text in Arc command responses instead. Name rules use server-side predicates so generated client validators do not short-circuit the localized server error with build-time English messages. Run the frontend and backend checks from [Local development](./local-development.md). Bokmål copy needs native review before release.
