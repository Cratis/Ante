// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { applyLocaleToDocument } from './applyLocaleToDocument';
import { DEFAULT_LOCALE, SupportedLocale } from './Locale';
import { LocaleSettings, negotiateLocale } from './negotiateLocale';

export const LOCALE_PREFERENCE_KEY = 'ante.locale.preference';
export const LOCALE_COOKIE_NAME = 'ante-locale';
let activeLocale: SupportedLocale = DEFAULT_LOCALE;

/** A stable header callback can read the current locale even on commands created before a switch. */
export const getActiveLocale = (): SupportedLocale => activeLocale;

/** Resolve before mounting Arc, including its observable connections and validation requests. */
export const applyInitialLocale = async (): Promise<{ locale: SupportedLocale; settings: LocaleSettings }> => {
    // Without the deployment's policy, only English is safe: an operator may have removed Bokmål from
    // Ante:SupportedLocales, and English is always shipped and the documented last resort.
    let settings: LocaleSettings = { defaultLocale: 'en', supportedLocales: ['en'] };
    try {
        const response = await fetch('/api/locale-config', { credentials: 'same-origin', signal: AbortSignal.timeout(3000) });
        if (response.ok) settings = await response.json() as LocaleSettings;
    } catch {
        // An unreachable policy keeps the English-only fallback above; the cookie aligns event transports.
    }
    let preference: string | null = null;
    try {
        preference = localStorage.getItem(LOCALE_PREFERENCE_KEY);
    } catch {
        // Storage may be unavailable; a page-lifetime selection remains possible.
    }
    const locale = negotiateLocale(
        preference,
        new URLSearchParams(window.location.search).get('lang'),
        navigator.languages?.length ? navigator.languages : [navigator.language],
        settings
    );
    applySelectedLocale(locale);
    return { locale, settings };
};

/** Update document and browser transports together, including a page-lifetime-only selection. */
export const applySelectedLocale = (locale: SupportedLocale): void => {
    activeLocale = locale;
    applyLocaleToDocument(document.documentElement, locale);
    try {
        document.cookie = `${LOCALE_COOKIE_NAME}=${locale}; Path=/; SameSite=Lax; Max-Age=31536000${window.location.protocol === 'https:' ? '; Secure' : ''}`;
    } catch {
        // Cookies can also be disabled; HTTP still uses the per-tab Arc header.
    }
};

/** Explicit user preference wins over link hints on the next load when storage is available. */
export const saveLocalePreference = (locale: SupportedLocale): void => {
    try {
        localStorage.setItem(LOCALE_PREFERENCE_KEY, locale);
    } catch {
        // A disabled storage API does not prevent changing the language for this page.
    }
};
