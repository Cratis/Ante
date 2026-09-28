// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { afterEach, beforeEach, vi } from 'vitest';
import { applyInitialLocale, applySelectedLocale } from '../applyInitialLocale';

describe('when loading the deployment locale before the first render', () => {
    let fetchSettings: sinon.SinonStub;
    let result: Awaited<ReturnType<typeof applyInitialLocale>>;
    let setAttribute: sinon.SinonSpy;

    beforeEach(async () => {
        fetchSettings = sinon.stub().resolves({ ok: true, json: async () => ({ defaultLocale: 'nb-NO', supportedLocales: ['en', 'nb-NO'] }) });
        setAttribute = sinon.spy();
        vi.stubGlobal('fetch', fetchSettings);
        vi.stubGlobal('localStorage', { getItem: () => null, setItem: () => {}, removeItem: () => {} });
        vi.stubGlobal('navigator', { languages: ['fr-FR'], language: 'fr-FR' });
        vi.stubGlobal('document', { documentElement: { setAttribute }, cookie: '' });
        vi.stubGlobal('window', { location: { search: '', protocol: 'http:' } });
        result = await applyInitialLocale();
    });

    afterEach(() => {
        applySelectedLocale('en');
        sinon.restore();
        vi.unstubAllGlobals();
    });

    it('should request the Vite-proxied API path', () => fetchSettings.firstCall.args[0].should.equal('/api/locale-config'));
    it('should bound the request by an abort signal', () => (fetchSettings.firstCall.args[1].signal instanceof AbortSignal).should.be.true);
    it('should apply the configured default when no visitor language matches', () => result.locale.should.equal('nb-NO'));
    it('should set the Bokmål document language', () => setAttribute.should.have.been.calledWith('lang', 'nb-NO'));
});
