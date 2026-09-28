// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, ReactNode, useContext } from 'react';
import { DEFAULT_LOCALE, SupportedLocale } from './Locale';
import { LocaleSettings } from './negotiateLocale';

const LocaleReactContext = createContext<SupportedLocale>(DEFAULT_LOCALE);
const LocaleSettingsContext = createContext<LocaleSettings>({ defaultLocale: 'en', supportedLocales: ['en', 'nb-NO'] });

interface LocaleProviderProps {
    /** The resolved locale to make available to descendants. */
    locale: SupportedLocale;
    settings: LocaleSettings;
    children: ReactNode;
}

/**
 * Makes the resolved locale (`Cratis/Ante#21`) available to any component that needs it for
 * locale-aware formatting - e.g. `useAccessibleStepper`'s number-formatted step announcements -
 * without prop-drilling it through every page. Mounted once in `index.tsx`, above `<App />`.
 */
export const LocaleProvider = ({ locale, settings, children }: LocaleProviderProps) => (
    <LocaleSettingsContext.Provider value={settings}>
        <LocaleReactContext.Provider value={locale}>{children}</LocaleReactContext.Provider>
    </LocaleSettingsContext.Provider>
);

/** Reads the currently active locale. Resolves to {@link DEFAULT_LOCALE} outside a {@link LocaleProvider} (e.g. in a spec). */
export const useLocale = (): SupportedLocale => useContext(LocaleReactContext);

/** Reads the deployment's configured language allowlist. */
export const useLocaleSettings = (): LocaleSettings => useContext(LocaleSettingsContext);
