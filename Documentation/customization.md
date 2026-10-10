---
title: Customize the lobby
description: Supply branding and legal documents, and configure English or Norwegian Bokmål.
---

Ante has URL-based branding, two legal-document sources, and two bundled languages. There is no deployed theme catalog, legal authoring UI or runtime locale setting.

## Set branding assets

1. Host a logo and stylesheet under URLs your lobby browser can reach, or mount static assets into Ante's `wwwroot` and use same-origin paths. Set `Ante:LogoUrl` and `Ante:CustomCssUrl` to those URLs. Empty values use the wordmark and default CSS.
2. Load the lobby through your actual ingress and check the browser's asset responses and Content-Security-Policy. The CSS loader accepts same-origin HTTP(S) or cross-origin **HTTPS**; it rejects malformed URLs, other schemes and cross-origin plain HTTP. A failed CSS load removes the link; a failed logo load displays the wordmark.
3. Check keyboard focus, contrast and layout after applying custom CSS. The URL check does **not** sanitize CSS; the operator controls trusted assets and any CSP `style-src` restrictions.

## Brand the loading screen

Before the application has loaded, a visitor sees a loading screen the server writes into the page: your logo (or the
`Ante:PageTitle` wordmark when there is none) above a spinner, on the page's own tab title. It is shaped on the server
because the application's own branding only arrives after its script has loaded.

- `Ante:PageTitle` sets the tab title, `Ante:LogoUrl` the logo and `Ante:SplashMessage` the text assistive technology announces.
- `Ante:CustomCssUrl` is linked in the page head, so it applies from the first paint. Set the custom properties
  `--ante-splash-background`, `--ante-splash-foreground`, `--ante-splash-accent` and `--ante-splash-logo-height`, or style
  `.ante-splash`, `.ante-splash__logo`, `.ante-splash__wordmark` and `.ante-splash__spinner` directly.
- `Ante:SplashHtml` replaces the screen entirely with markup of your own. It is written into the page as it is, so it must
  come from your deployment configuration, never from user input.

The stylesheet and logo addresses must be a path on the same host (`/branding/lobby.css`) or an `https` URL.

## Describe the registration offer

Set `Ante:Registration:Content:{locale}` to explain a self-service signup on `/register`, for example:

```json
"Ante": {
  "Registration": {
    "Content": {
      "en": {
        "Title": "Start your 14-day free trial",
        "Intro": "Create your organization and try everything for 14 days. No credit card required.",
        "Highlights": [ "Invite your team after choosing a plan", "Cancel any time" ],
        "PricingUrl": "https://example.com/pricing",
        "LoginUrl": "https://app.example.com/",
        "CompletionMessage": "Setting up your organization..."
      }
    }
  }
}
```

Every value is plain text; markup is shown literally. Links must be https URLs or same-origin paths, otherwise startup fails. Ante picks the entry for the visitor's locale (`nb-NO`, then `nb`), then `en`; with no entry the page looks as it does without the setting. `Title` replaces the subtitle and `CompletionMessage` replaces the waiting and success text.

## Supply legal documents

Choose one source for the deployment:

- **InProcess (default):** If you own a custom Ante composition, implement `Ante.Legal.ILegalDocumentSource.GetCurrent()` returning `Task<LegalDocumentSet?>`. Its result has `TermsAndConditions: LegalDocumentBody`, `PrivacyPolicy: LegalDocumentBody` and `Version: LegalVersion`; return `null` when nothing is offered. Register it in **Ante's process** before serving traffic. Stock `Program.cs` installs `NoLegalDocumentSource`; registering an implementation in a separately deployed host does nothing. InProcess `null` means no legal step.
- **Inbox:** Set `Ante:Legal:Source=Inbox`, `Ante:Legal:PublisherStore` to an entry in `Ante:HostStores`, and `Ante:Legal:DocumentSetId` to a stable non-GUID event-source id. The publisher writes a full `LegalDocumentSetPublished` snapshot to that store's **outbox** under that id. Ante receives and activates it on the local legal stream, then publishes `LegalDocumentSetActivated` to its outbox. Start Ante in Inbox mode and confirm the publisher store's outbox-to-Ante subscription includes `LegalDocumentSetPublished` **before** the host publishes the set. In Chronicle 19.13.1 (not rechecked on later kernels, so assume it still holds), adding this type to an existing subscription does not backfill earlier outbox events of that type; if no set is active, republish the current set after confirming the filter. An identical republish is a harmless no-op if it was already activated. Install the host's reverse acknowledgement subscription as well. Before the first activation, the legal query reports `IsUnavailable=true` and all three onboarding submissions are blocked; there is no in-process or bundled fallback. Use the same namespace on host and Ante.

Verify the current documents appear as a terms step in all three wizards. On submission Ante validates acceptance against the current activated version and emits `LegalTermsAccepted` only when documents were actually presented and accepted. Change content and version together; a stale submission is rejected. In Inbox mode, each publication is a complete immutable pair with a strictly increasing positive revision; the host keeps the version-to-text archive. An older delivery never rolls back the current set, identical duplicates do nothing, and contradictory reuse is recorded locally as `LegalDocumentSetRejected`. The newest *activated* revision is current, not the newest host draft or an in-flight publication. See [Host integration](./host-integration.md) and [Operations](./operations.md) for delivery and diagnosis.

## Choose the lobby language

English (`en`, rendered as `en-US`) and Norwegian Bokmål (`nb-NO`) ship with matching frontend and server messages. `nb` and `nb-NO` select Bokmål; `no` is accepted as a Bokmål compatibility alias, **not** Nynorsk (`nn`). Set `Ante:DefaultLocale` and `Ante:SupportedLocales` to choose the deployment default and limit what visitors can select; see [Configuration](./configuration.md#ante-runtime-options). Only shipped locales are accepted, and the default must be in the allowlist.

The visitor's choice in Display preferences persists in their browser when storage is available and takes precedence over a `?lang=nb-NO` link hint. Without browser storage, the choice lasts for the current page without a reload. Otherwise Ante picks the first allowed language in the browser's ordered language list, then the configured default, then English. Unsupported hints are ignored. Ante fetches its locale policy before rendering and sends the resolved language in `Accept-Language` on Arc HTTP requests. It also writes a same-origin `ante-locale` cookie (`SameSite=Lax`) before Arc connects: browser WebSocket/EventSource transports cannot set custom headers. The server uses `Accept-Language` for Arc HTTP requests, even when another tab has written a conflicting cookie. The cookie is only preferred for WebSocket and EventSource connections. Numeric parsing and identity comparisons remain invariant; only display messages change.

If you also serve sign-in or invitation email through AuthProxy, supply matching proxy-owned translations: Ante does not localize those pages or emails, nor can it guarantee a query hint survives an external sign-in redirect. Browser language still works on return. Legal text and its revision remain entirely host-owned; Ante never translates or substitutes it.

## Add a language in source

A new locale needs a translation with exactly the English keys under `Source/Ante/Locales/`, entries in `Source/Ante/Locale/Locale.ts` and negotiation, matching server resources under `Source/Ante/Resources/`, and the parity/spec checks. Chronicle freezes constraint messages when definitions are registered, outside a request; Ante maps known constraint names to localized text in Arc command responses instead. Name rules use server-side predicates so generated client validators do not short-circuit the localized server error with build-time English messages. Run the frontend and backend checks from [Local development](./local-development.md). Bokmål copy needs native review before release.
