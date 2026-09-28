// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { afterEach, beforeEach, vi } from 'vitest';
import { isLocalePreferenceAvailable, saveLocalePreference } from '../applyInitialLocale';

describe('when browser storage cannot persist a language choice', () => {
    let reload: sinon.SinonSpy;
    let setItem: sinon.SinonStub;

    beforeEach(() => {
        reload = sinon.spy();
        setItem = sinon.stub().throws(new Error('Storage disabled'));
        vi.stubGlobal('localStorage', { setItem, removeItem: sinon.spy() });
        vi.stubGlobal('window', { location: { reload } });
    });

    afterEach(() => {
        sinon.restore();
        vi.unstubAllGlobals();
    });

    it('should hide the language option', () => isLocalePreferenceAvailable().should.be.false);
    it('should not reload into the old locale after a failed save', () => {
        saveLocalePreference('nb-NO');
        reload.should.not.have.been.called;
    });
});
