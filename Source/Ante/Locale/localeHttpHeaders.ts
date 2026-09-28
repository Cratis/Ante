// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { LOCALE_TAGS } from './Locale';
import { getActiveLocale } from './applyInitialLocale';

/** Used by Arc and the standalone name probes; reads the current tab's locale on every request. */
export const localeHttpHeaders = () => ({ 'Accept-Language': LOCALE_TAGS[getActiveLocale()] });
