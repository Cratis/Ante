// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import strings, { selectStrings } from '../../Locales/Strings';
import { LocaleProvider, useLocale } from '../LocaleContext';
import { applySelectedLocale } from '../applyInitialLocale';
import { changeLocale } from '../changeLocale';
import { localeHttpHeaders } from '../localeHttpHeaders';

const CurrentLanguage = () => React.createElement('span', null, useLocale(), ': ', strings.components.stepper.next);

describe('when browser storage cannot persist a language choice', () => {
    const setAttribute = vi.fn();
    const reload = vi.fn();
    const storage = { setItem: vi.fn(() => { throw new Error('Storage disabled'); }) };
    const documentStub = { documentElement: { setAttribute }, cookie: '' };
    const updateProvider = vi.fn();

    beforeEach(() => {
        vi.stubGlobal('localStorage', storage);
        vi.stubGlobal('window', { location: { protocol: 'https:', reload } });
        vi.stubGlobal('document', documentStub);
        selectStrings('en');
        changeLocale('nb-NO', updateProvider);
    });

    afterEach(() => {
        selectStrings('en');
        applySelectedLocale('en');
        vi.unstubAllGlobals();
        vi.clearAllMocks();
    });

    it('should render the selected language in the page lifetime provider and Components messages', () => {
        updateProvider.mock.calls.should.deep.include(['nb-NO']);
        renderToStaticMarkup(React.createElement(LocaleProvider, {
            locale: 'nb-NO', settings: { defaultLocale: 'en', supportedLocales: ['en', 'nb-NO'] }, onChange: vi.fn()
        }, React.createElement(CurrentLanguage))).should.include('nb-NO: Neste');
    });
    it('should update document language and event transport cookie without reloading', () => {
        setAttribute.mock.calls.should.deep.include(['lang', 'nb-NO']);
        documentStub.cookie.should.include('ante-locale=nb-NO');
        reload.mock.calls.should.have.lengthOf(0);
    });
    it('should send the new locale on Arc HTTP requests', () => {
        localeHttpHeaders()['Accept-Language'].should.equal('nb-NO');
    });
});
