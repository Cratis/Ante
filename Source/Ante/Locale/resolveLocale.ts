// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DEFAULT_LOCALE, SupportedLocale } from './Locale';
import { normalizeLocale } from './negotiateLocale';

/**
 * Resolves a requested locale - e.g. `navigator.language`, or any other source of a browser/user
 * -expressed preference - against Ante's supported-locale policy (`Cratis/Ante#21`).
 *
 * Deterministic and total: matches shipped primary language subtags (including `no` as an alias
 * for Bokmål). Unsupported tags fall back to {@link DEFAULT_LOCALE}. For the full preference/link/
 * browser/deployment precedence, use `negotiateLocale`.
 * @param requested The requested locale tag (e.g. `en`, `en-GB`), or `null`/`undefined` when nothing was expressed.
 * @returns A {@link SupportedLocale} - never throws, never returns an unsupported value.
 */
export const resolveLocale = (requested: string | null | undefined): SupportedLocale => {
    return normalizeLocale(requested) ?? DEFAULT_LOCALE;
};
