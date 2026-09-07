// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Validates and normalizes a host-configured `Ante:CustomCssUrl` value (`Cratis/Ante#21`) into an
 * absolute URL safe to load as a `<link rel="stylesheet">`, or `null` when nothing should load.
 *
 * `CustomCssUrl` is deployment-owned configuration - the same trust level as `Ante:LogoUrl` or
 * `Ante:HostAppUrl` - not end-user input, so this is not a sanitizer against a hostile value; Ante
 * does not (and cannot) sanitize the CSS a deployment points at, and arbitrary trusted CSS can still
 * affect accessibility - that trust boundary is documented, not hidden, in
 * `Documentation/boundaries.md#branding-presets`. What this **does** guarantee, regardless of how the
 * value was typed or misconfigured:
 * - Empty/whitespace-only resolves to `null` - the default styling applies, never a broken load.
 * - Any scheme other than `http:`/`https:` (`javascript:`, `data:`, `file:`, ...) resolves to `null`
 *   unconditionally - never becomes a stylesheet regardless of origin.
 * - A same-origin value (e.g. a file mounted into Ante's own `wwwroot`, referenced by a root-relative
 *   path) is always allowed, at whatever scheme the document itself was served over.
 * - A cross-origin value is allowed only over `https:` - a plain-`http:` third party is refused rather
 *   than silently loaded over an unencrypted connection.
 * - A value `URL` cannot parse at all resolves to `null` rather than throwing.
 *
 * A deployment wanting finer-grained control over which cross-origin hosts may serve this stylesheet
 * enforces it with its own Content-Security-Policy `style-src` directive - the browser honors that
 * independently of anything here. Ante does not maintain its own per-origin allow-list; it does not
 * know a deployment's real policy, and inventing one here would be exactly the "fictitious
 * sanitization guarantee" the issue warns against.
 * @param rawUrl The configured `customCssUrl`, as returned by `BrandingConfiguration`.
 * @param documentUrl The current document's URL - same-origin/relative resolution happens against this.
 * @returns An absolute URL safe to load, or `null`.
 */
export const resolveTrustedCssUrl = (rawUrl: string | null | undefined, documentUrl: string): string | null => {
    const trimmed = (rawUrl ?? '').trim();
    if (!trimmed) return null;

    try {
        const resolved = new URL(trimmed, documentUrl);
        if (resolved.protocol !== 'https:' && resolved.protocol !== 'http:') return null;

        const isSameOrigin = resolved.origin === new URL(documentUrl).origin;
        if (!isSameOrigin && resolved.protocol !== 'https:') return null;

        return resolved.href;
    } catch {
        return null;
    }
};
