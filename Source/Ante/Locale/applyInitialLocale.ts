// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { applyLocaleToDocument } from './applyLocaleToDocument';
import { SupportedLocale } from './Locale';
import { resolveLocale } from './resolveLocale';

/**
 * Resolves the browser's requested locale and applies it to the document root synchronously, before
 * React renders anything - called once from `index.tsx`, ahead of `ReactDOM.createRoot(...).render(...)`,
 * the same "no unwanted first-paint flash" reasoning `applyInitialDisplayPreferences` documents for
 * display preferences (`Cratis/Ante#20`).
 *
 * Returns the resolved locale so `index.tsx` can also thread it into `CratisComponentsProvider`'s
 * `locale` (React Aria's own dates/numbers/announcements) and `LocaleProvider` (for
 * `useAccessibleStepper`'s locale-aware step announcements) - one resolution, applied consistently
 * everywhere the lobby needs it.
 * @returns The resolved {@link SupportedLocale}.
 */
export const applyInitialLocale = (): SupportedLocale => {
    const locale = resolveLocale(globalThis.navigator?.language);
    applyLocaleToDocument(document.documentElement, locale);
    return locale;
};
