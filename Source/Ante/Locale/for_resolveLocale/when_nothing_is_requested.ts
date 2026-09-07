// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DEFAULT_LOCALE } from '../Locale';
import { resolveLocale } from '../resolveLocale';

describe('when nothing is requested', () => {
    it('should fall back to the default locale for undefined', () => resolveLocale(undefined).should.equal(DEFAULT_LOCALE));

    it('should fall back to the default locale for null', () => resolveLocale(null).should.equal(DEFAULT_LOCALE));

    it('should fall back to the default locale for an empty string', () => resolveLocale('').should.equal(DEFAULT_LOCALE));

    it('should fall back to the default locale for whitespace only', () => resolveLocale('   ').should.equal(DEFAULT_LOCALE));
});
