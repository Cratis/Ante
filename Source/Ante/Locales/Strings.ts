// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import translationEN from './en/translation.json';
import translationNB from './nb/translation.json';
import { SupportedLocale } from '../Locale/Locale';

const strings = { ...translationEN };

/** Keep the same object for existing imports while replacing messages for the active locale. */
export const selectStrings = (locale: SupportedLocale): void => {
    Object.assign(strings, locale === 'nb-NO' ? translationNB : translationEN);
};

export default strings;
