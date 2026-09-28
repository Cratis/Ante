// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { selectStrings } from '../Locales/Strings';
import { SupportedLocale } from './Locale';
import { applySelectedLocale, saveLocalePreference } from './applyInitialLocale';

/** Apply one selection to UI messages, provider state, document and browser transports. */
export const changeLocale = (locale: SupportedLocale, updateProvider: (locale: SupportedLocale) => void): void => {
    saveLocalePreference(locale);
    selectStrings(locale);
    applySelectedLocale(locale);
    updateProvider(locale);
};
