// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { afterEach, beforeEach, vi } from 'vitest';
import { applyInitialLocale, applySelectedLocale } from '../applyInitialLocale';

describe('when the deployment locale settings cannot be loaded', () => {
    let result: Awaited<ReturnType<typeof applyInitialLocale>>;

    beforeEach(async () => {
        vi.stubGlobal('fetch', sinon.stub().rejects(new Error('offline')));
        vi.stubGlobal('localStorage', { getItem: () => 'nb-NO', setItem: () => {}, removeItem: () => {} });
        vi.stubGlobal('navigator', { languages: ['nb-NO'], language: 'nb-NO' });
        vi.stubGlobal('document', { documentElement: { setAttribute: sinon.spy() }, cookie: '' });
        vi.stubGlobal('window', { location: { search: '?lang=nb-NO', protocol: 'http:' } });
        result = await applyInitialLocale();
    });

    afterEach(() => {
        applySelectedLocale('en');
        sinon.restore();
        vi.unstubAllGlobals();
    });

    it('should fall back to English only', () => result.settings.supportedLocales.should.deep.equal(['en']));
    it('should render in English even when the visitor prefers Bokmål', () => result.locale.should.equal('en'));
});

describe('when the deployment locale settings are malformed', () => {
    let result: Awaited<ReturnType<typeof applyInitialLocale>>;

    beforeEach(async () => {
        vi.stubGlobal('fetch', sinon.stub().resolves({ ok: true, json: async () => ({ defaultLocale: 42, supportedLocales: 'nb-NO' }) }));
        vi.stubGlobal('localStorage', { getItem: () => 'nb-NO', setItem: () => {}, removeItem: () => {} });
        vi.stubGlobal('navigator', { languages: ['nb-NO'], language: 'nb-NO' });
        vi.stubGlobal('document', { documentElement: { setAttribute: sinon.spy() }, cookie: '' });
        vi.stubGlobal('window', { location: { search: '', protocol: 'http:' } });
        result = await applyInitialLocale();
    });

    afterEach(() => {
        applySelectedLocale('en');
        sinon.restore();
        vi.unstubAllGlobals();
    });

    it('should keep the English-only fallback', () => result.settings.supportedLocales.should.deep.equal(['en']));
    it('should render in English', () => result.locale.should.equal('en'));
});
