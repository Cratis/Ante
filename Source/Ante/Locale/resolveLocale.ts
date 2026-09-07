// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DEFAULT_LOCALE, SUPPORTED_LOCALES, SupportedLocale } from './Locale';

const isSupported = (candidate: string): candidate is SupportedLocale =>
    (SUPPORTED_LOCALES as readonly string[]).includes(candidate);

/**
 * Resolves a requested locale - e.g. `navigator.language`, or any other source of a browser/user
 * -expressed preference - against Ante's supported-locale policy (`Cratis/Ante#21`).
 *
 * Deterministic and total: matches the primary language subtag only (case-insensitively, ignoring any
 * region/script subtag), against {@link SUPPORTED_LOCALES}. Anything unset, malformed, or not on that
 * list falls back to {@link DEFAULT_LOCALE} - there is no partial match beyond the primary subtag, no
 * locale negotiation, and deliberately no development-only override: the same input always resolves to
 * the same locale, in every environment, so an unreviewed translation can never reach production
 * through a hidden switch.
 * @param requested The requested locale tag (e.g. `en`, `en-GB`), or `null`/`undefined` when nothing was expressed.
 * @returns A {@link SupportedLocale} - never throws, never returns an unsupported value.
 */
export const resolveLocale = (requested: string | null | undefined): SupportedLocale => {
    const primarySubtag = (requested ?? '').trim().toLowerCase().split(/[-_]/)[0];
    return isSupported(primarySubtag) ? primarySubtag : DEFAULT_LOCALE;
};
