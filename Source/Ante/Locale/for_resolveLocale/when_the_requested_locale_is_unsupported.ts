// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DEFAULT_LOCALE } from '../Locale';
import { resolveLocale } from '../resolveLocale';

describe('when the requested locale is not one Ante ships reviewed translations for', () => {
    it('should fall back to the default locale deterministically', () => resolveLocale('fr').should.equal(DEFAULT_LOCALE));

    it('should fall back even for a plausible-looking but unreviewed locale', () => resolveLocale('de-DE').should.equal(DEFAULT_LOCALE));
});
