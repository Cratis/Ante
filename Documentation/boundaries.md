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

A deployment that wants more than that can opt into `Ante:HostOutcomeUrl` (see [Host Integration: the host outcome backchannel](./host-integration.md#the-host-outcome-backchannel)) — a purely informational, authenticated, attempt-bound lookup of what a host reports happened afterward, shown on the join and invited-organization-creation wizards' completion screen without ever blocking access to the host or rewriting Ante's own published outcome. It is not host membership, grants, or provisioning by another name: Ante still never waits for it, a host that implements nothing continues to work exactly as before, and it does not cover self-service registration (no verified-owner session exists for a bare registration id).

## Admin invite-authoring UI

Deciding *who* to invite, with what role, into which tenant, is a host concern. Ante only reacts to the invitation events a host already decided to append.

## Multi-tenancy of Ante itself

Ante runs single-tenant, in a single fixed Chronicle namespace per instance — `Default` unless `Ante:Namespace` overrides it, and never resolved per request. A product needing several isolated lobbies runs several Ante instances, differentiated by `Ante:EventStore` and/or `Ante:Namespace` (see [Configuration](./configuration.md#example-a-named-lobby-instance)) — not by adding request-scoped tenancy inside Ante. `FixedNamespaceResolver` reflects this directly: it always returns the one namespace an instance was configured with, unlike Chronicle's `ClaimsBasedNamespaceResolver`, which is built for exactly the per-request resolution Ante deliberately does not do.

## Host membership and grants

Ante resolves *who is onboarding* and hands the host a correlated accepted/registered event with the invitee's name, email, and identity-provider subject. What roles or grants that translates to inside the host's own authorization model is entirely the host's decision.

## Branding presets

`Ante:LogoUrl` and `Ante:CustomCssUrl` let a deployment point at its own logo and stylesheet, but Ante ships no built-in theme catalog or design-system integration beyond that. A host wanting a fully branded lobby supplies its own assets through those two settings.

`Ante:CustomCssUrl` is loaded through a real `<link rel="stylesheet">`, appended once, deterministically, after the lobby's own built-in styles — never `dangerouslySetInnerHTML` of fetched CSS text — so the browser, and the deployment's own Content-Security-Policy, govern the request exactly as they would any other stylesheet. It is refused outright (falling back to the default styling) when it cannot be parsed as a URL, when it uses a scheme other than `http:`/`https:`, or when it is cross-origin over plain `http:`; a same-origin value (e.g. a file mounted into Ante's own `wwwroot`) is always allowed, at whatever scheme the document itself was served over. A configured value that loads but fails afterwards (blocked, 404, network failure) is removed and logged to the console, never surfaced to the person using the lobby.

**This is the honest limit of that guarantee, stated rather than left implicit:** `CustomCssUrl` is deployment-owned configuration — the same trust level as `Ante:HostAppUrl` or `Ante:LogoUrl` — not end-user input, so none of the above is sanitization against a hostile value. Ante does not, and cannot, sanitize the CSS content itself; arbitrary trusted CSS can still affect accessibility (contrast, focus visibility, layout) once it loads, and that is the deploying operator's responsibility to get right, not something Ante can verify. Ante also does not maintain its own per-origin allow-list for cross-origin stylesheets — a deployment wanting finer-grained control enforces it with its own Content-Security-Policy `style-src` directive, which the browser honors independently.

A configured `Ante:LogoUrl` that fails to load falls back to the plain "Ante" text wordmark rather than the browser's broken-image icon, the same neutral-default philosophy the accessibility work in [#20](https://github.com/Cratis/Ante/issues/20) applies to display preferences.

## Localization

The lobby ships one supported UI locale today: English (`en`). This is a deliberate, documented scope boundary, not an oversight — see [Configuration: Locale](./configuration.md#locale) for why the frontend and backend cannot drift apart on language while that remains true.

Adding a second locale is real work, not just a translation file: it needs reviewed human translations (not machine translation, and not another product's roster copy-pasted in), the locale added to the frontend's closed `SUPPORTED_LOCALES` set, **and** the backend's own validation messages localized to match — `Program.cs` currently pins the whole backend to `CultureInfo.InvariantCulture` deliberately, so introducing a second frontend locale without also localizing the backend would let the interface and the server disagree about what language a rejection is in. There is deliberately no development-only locale override to preview an unreviewed translation outside that process.

A locale change is a UI-language change only. Legal content and its version are always host-authoritative through `ILegalDocumentSource`, regardless of which locale the interface is rendering in — switching the interface language must never, by itself, invent a new legal version or force re-consent; only the host's own document content actually changing does that (see [Invitation Lifecycle: Legal consent](./invitation-lifecycle.md#legal-consent)).

## Render failure recovery

A render error inside the lobby (a bug in a component, a provider failing to initialize) degrades to a minimal, safe-language recovery screen with a reload action — never a blank or half-rendered page — via a single boundary wrapping the entire application, including the branding and component-library providers themselves. It never displays the caught error's message or stack trace, since a render failure can be carrying request/form state that must not be echoed back as diagnostic text; the real error is still logged to the console for whoever is watching devtools or server logs. This boundary catches render errors only — network and command failures already have their own explicit journey states (see [Wizards: Status polling, recovery, and redirect](./wizards.md#status-polling-recovery-and-redirect)) and are never routed through it.

## Next steps

- [Host Integration](./host-integration.md) — what Ante does hand the host, precisely.
- [Index](./index.md) — the full map of this documentation set.
