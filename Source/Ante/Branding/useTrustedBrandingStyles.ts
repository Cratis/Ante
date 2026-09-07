// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect } from 'react';
import { resolveTrustedCssUrl } from './resolveTrustedCssUrl';

/** The `id` the injected `<link>` carries, so a re-render/re-mount can find and replace it instead of accumulating duplicates. */
export const TRUSTED_CSS_LINK_ID = 'ante-trusted-branding-css';

/**
 * The one lifecycle-managed loader for `Ante:CustomCssUrl` (`Cratis/Ante#21`) - the frontend consumer
 * `Documentation/configuration.md` and `Documentation/boundaries.md#branding-presets` promised but the
 * lobby never actually had.
 *
 * Loads through a real `<link rel="stylesheet">` - never `dangerouslySetInnerHTML` of fetched CSS text
 * - so the browser, and the deployment's own Content-Security-Policy, govern the request exactly as
 * they would any other stylesheet. See {@link resolveTrustedCssUrl} for what is and is not allowed to
 * load.
 *
 * - **Deterministic ordering** - always appended at the end of `<head>`, after the build's own static
 *   stylesheet imports (`@cratis/components`, PrimeReact, `index.css`), so custom branding can always
 *   override the default look through normal cascade order regardless of render timing.
 * - **Deduplication** - keyed by a fixed element id; an unchanged URL across a re-render is a no-op, a
 *   changed one replaces the existing `<link>` rather than accumulating another.
 * - **Failure fallback** - a `<link>` that fails to load (404, blocked by CSP, network failure) is
 *   removed and logged to the console, never surfaced to the person using the lobby - leaving the
 *   default Cratis Components/PrimeReact styling in place rather than a half-applied or broken page.
 * @param customCssUrl The raw `customCssUrl` from `BrandingConfiguration`, or `undefined` while the query is still loading / not configured.
 */
export const useTrustedBrandingStyles = (customCssUrl: string | null | undefined): void => {
    useEffect(() => {
        const resolved = resolveTrustedCssUrl(customCssUrl, window.location.href);
        const existing = document.getElementById(TRUSTED_CSS_LINK_ID) as HTMLLinkElement | null;

        if (!resolved) {
            existing?.remove();
            return;
        }

        if (existing?.href === resolved) return;

        existing?.remove();

        const link = document.createElement('link');
        link.id = TRUSTED_CSS_LINK_ID;
        link.rel = 'stylesheet';
        link.href = resolved;
        link.addEventListener('error', () => {
            link.remove();
            console.error('Ante: the configured custom branding stylesheet failed to load - continuing with the default styling.');
        });
        document.head.appendChild(link);
    }, [customCssUrl]);
};
