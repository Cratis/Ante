// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, vi } from 'vitest';
import { captureSignupContext, clearSignupContext } from '../../signupContext';
import { FakeSessionStorage } from '../../for_RegistrationOperation/FakeSessionStorage';

describe('when capturing signup context and a later load has no query', () => {
    beforeEach(() => {
        vi.stubGlobal('sessionStorage', new FakeSessionStorage());
        captureSignupContext(['offer'], '?offer=trial');
    });

    afterEach(() => vi.unstubAllGlobals());

    it('should keep what the first link carried', () =>
        captureSignupContext(['offer'], '').map(entry => entry.value).should.deep.equal(['trial']));

    it('should forget it once cleared', () => {
        clearSignupContext();
        captureSignupContext(['offer'], '').should.be.empty;
    });

    it('should drop kept keys the deployment no longer allows', () =>
        captureSignupContext(['utm_source'], '').should.be.empty);
});
