// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DEFAULT_LOCALE, SUPPORTED_LOCALES, SupportedLocale } from './Locale';

export interface LocaleSettings {
    defaultLocale: string;
    supportedLocales: string[];
}

/** Recognize only shipped primary tags; 'no' is Bokmål, never Nynorsk. */
export const normalizeLocale = (tag: string | null | undefined): SupportedLocale | undefined => {
    const primary = tag?.trim().toLowerCase().split(/[-_]/)[0];
    if (primary === 'nb' || primary === 'no') return 'nb-NO';
    if (primary === 'en') return 'en';
    return undefined;
};

export const allowedLocales = (settings: LocaleSettings): SupportedLocale[] =>
    settings.supportedLocales.map(normalizeLocale).filter((locale): locale is SupportedLocale =>
        locale !== undefined && SUPPORTED_LOCALES.includes(locale));

/** A rejected hint must not suppress later sources (including later browser languages). */
export const negotiateLocale = (
    preference: string | null | undefined,
    hint: string | null | undefined,
    languages: readonly string[],
    settings: LocaleSettings
): SupportedLocale => {
    const allowed = allowedLocales(settings);
    for (const candidate of [preference, hint, ...languages, settings.defaultLocale, DEFAULT_LOCALE]) {
        const locale = normalizeLocale(candidate);
        if (locale && allowed.includes(locale)) return locale;
    }
    // Invalid/empty deployment policy is rejected at server startup; an offline browser
    // still has a usable English fallback.
    return DEFAULT_LOCALE;
};
