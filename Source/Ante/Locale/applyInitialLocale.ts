// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { applyLocaleToDocument } from './applyLocaleToDocument';
import { SupportedLocale } from './Locale';
import { LocaleSettings, negotiateLocale } from './negotiateLocale';

export const LOCALE_PREFERENCE_KEY = 'ante.locale.preference';
export const LOCALE_COOKIE_NAME = 'ante-locale';

/** Resolve before mounting Arc, including its observable connections and validation requests. */
export const applyInitialLocale = async (): Promise<{ locale: SupportedLocale; settings: LocaleSettings }> => {
    let settings: LocaleSettings = { defaultLocale: 'en', supportedLocales: ['en', 'nb-NO'] };
    try {
        const response = await fetch('/api/locale-config', { credentials: 'same-origin', signal: AbortSignal.timeout(3000) });
        if (response.ok) settings = await response.json() as LocaleSettings;
    } catch {
        // An offline bootstrap can still render English; the cookie aligns event transports.
    }
    let preference: string | null = null;
    if (isLocalePreferenceAvailable()) {
        preference = localStorage.getItem(LOCALE_PREFERENCE_KEY);
    }
    const locale = negotiateLocale(
        preference,
        new URLSearchParams(window.location.search).get('lang'),
        navigator.languages?.length ? navigator.languages : [navigator.language],
        settings
    );
    applyLocaleToDocument(document.documentElement, locale);
    document.cookie = `${LOCALE_COOKIE_NAME}=${locale}; Path=/; SameSite=Lax; Max-Age=31536000${window.location.protocol === 'https:' ? '; Secure' : ''}`;
    return { locale, settings };
};

/** A language switch requires durable storage; a transport cookie is not a preference. */
export const isLocalePreferenceAvailable = (): boolean => {
    try {
        const probe = `${LOCALE_PREFERENCE_KEY}.probe`;
        localStorage.setItem(probe, probe);
        localStorage.getItem(probe);
        localStorage.removeItem(probe);
        return true;
    } catch {
        return false;
    }
};

/** Explicit user preference wins over link hints on the next load. */
export const saveLocalePreference = (locale: SupportedLocale): void => {
    try {
        localStorage.setItem(LOCALE_PREFERENCE_KEY, locale);
    } catch {
        // Storage was disabled after the menu opened; do not reload into the previous locale.
        return;
    }
    window.location.reload();
};
