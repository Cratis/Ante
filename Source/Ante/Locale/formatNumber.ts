// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { LOCALE_TAGS, SupportedLocale } from './Locale';

/**
 * Formats a plain integer using the given locale's own digit-grouping conventions, rather than a
 * naive `String(value)` - the "number formatting where shown" half of `Cratis/Ante#21`'s locale
 * coordination. Used for the numbers the lobby actually displays today (stepper step counts, e.g.
 * "Step 2 of 3") - any future number the UI shows should go through this rather than string
 * concatenation, so it stays correct for whichever locale ends up added to `SUPPORTED_LOCALES`.
 * @param value The integer to format.
 * @param locale The locale to format it for.
 * @returns The locale-formatted number.
 */
export const formatNumber = (value: number, locale: SupportedLocale): string =>
    new Intl.NumberFormat(LOCALE_TAGS[locale]).format(value);
