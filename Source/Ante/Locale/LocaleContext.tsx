// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, ReactNode, useContext } from 'react';
import { DEFAULT_LOCALE, SupportedLocale } from './Locale';
import { LocaleSettings } from './negotiateLocale';

const LocaleReactContext = createContext<SupportedLocale>(DEFAULT_LOCALE);
const LocaleSettingsContext = createContext<LocaleSettings>({ defaultLocale: 'en', supportedLocales: ['en', 'nb-NO'] });
const LocaleChangeContext = createContext<(locale: SupportedLocale) => void>(() => {
    throw new Error('Language selection requires a LocaleProvider');
});

interface LocaleProviderProps {
    /** The resolved locale to make available to descendants. */
    locale: SupportedLocale;
    settings: LocaleSettings;
    onChange: (locale: SupportedLocale) => void;
    children?: ReactNode;
}

/**
 * Makes the active locale (`Cratis/Ante#21`) available for locale-aware formatting and
 * language changes without prop-drilling it through every page. Mounted in `index.tsx` above `<App />`.
 */
export const LocaleProvider = ({ locale, settings, onChange, children }: LocaleProviderProps) => (
    <LocaleSettingsContext.Provider value={settings}>
        <LocaleReactContext.Provider value={locale}>
            <LocaleChangeContext.Provider value={onChange}>{children}</LocaleChangeContext.Provider>
        </LocaleReactContext.Provider>
    </LocaleSettingsContext.Provider>
);

/** Reads the currently active locale. Resolves to {@link DEFAULT_LOCALE} outside a {@link LocaleProvider} (e.g. in a spec). */
export const useLocale = (): SupportedLocale => useContext(LocaleReactContext);

/** Reads the deployment's configured language allowlist. */
export const useLocaleSettings = (): LocaleSettings => useContext(LocaleSettingsContext);

/** Change the language without discarding this page's unsaved form state. */
export const useChangeLocale = (): ((locale: SupportedLocale) => void) => useContext(LocaleChangeContext);
