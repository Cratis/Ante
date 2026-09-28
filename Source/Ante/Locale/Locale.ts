// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** Languages with bundled frontend and backend messages. */
export const SUPPORTED_LOCALES = ['en', 'nb-NO'] as const;

/** One of the locales in {@link SUPPORTED_LOCALES}. */
export type SupportedLocale = (typeof SUPPORTED_LOCALES)[number];

/** The locale used whenever nothing requested is recognized - see {@link resolveLocale}. */
export const DEFAULT_LOCALE: SupportedLocale = 'en';

/**
 * The full BCP 47 tag each {@link SupportedLocale} resolves to for `Intl` formatting and for
 * `CratisComponentsProvider`'s `locale` (which React Aria uses for its own dates/numbers/interaction
 * announcements) - kept distinct from the bare language subtag so a future locale can pick a specific
 * regional variant (e.g. `fr` -> `fr-FR`) without changing the closed set above.
 */
export const LOCALE_TAGS: Record<SupportedLocale, string> = {
    en: 'en-US',
    'nb-NO': 'nb-NO'
};

/** Text direction for each {@link SupportedLocale}, applied to the document root - see `applyLocaleToDocument.ts`. */
export const LOCALE_DIRECTIONS: Record<SupportedLocale, 'ltr' | 'rtl'> = {
    en: 'ltr',
    'nb-NO': 'ltr'
};
