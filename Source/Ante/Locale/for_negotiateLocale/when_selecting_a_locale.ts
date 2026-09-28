// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { negotiateLocale, normalizeLocale } from '../negotiateLocale';

const settings = { defaultLocale: 'en', supportedLocales: ['en', 'nb-NO'] };

describe('when selecting an onboarding locale', () => {
    it('should use the persisted preference before a link hint', () =>
        negotiateLocale('nb-NO', 'en', ['en-US'], settings).should.equal('nb-NO'));

    it('should accept a link hint before the browser languages', () =>
        negotiateLocale(null, 'no', ['en-GB'], settings).should.equal('nb-NO'));

    it('should use the first supported language in browser order', () =>
        negotiateLocale(null, null, ['fr-FR', 'nb-NO', 'en-US'], settings).should.equal('nb-NO'));

    it('should skip an unsupported or disallowed hint and preference', () =>
        negotiateLocale('nn-NO', 'nb', ['nb-NO', 'en-US'], { ...settings, supportedLocales: ['en'] }).should.equal('en'));

    it('should fall back to the configured default and then English', () => {
        negotiateLocale(null, null, ['fr-FR'], { ...settings, defaultLocale: 'nb' }).should.equal('nb-NO');
        negotiateLocale(null, null, ['fr-FR'], settings).should.equal('en');
    });

    it('should treat no as Bokmål but not treat Nynorsk as Bokmål', () => {
        String(normalizeLocale('NO_no')).should.equal('nb-NO');
        (normalizeLocale('nn-NO') === undefined).should.be.true;
    });
});
