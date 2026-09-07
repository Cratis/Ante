// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, vi } from 'vitest';
import { clearRegistrationOperation, getOrCreateRegistrationId } from '../../RegistrationOperation';
import { FakeSessionStorage } from '../FakeSessionStorage';

describe('when clearing the operation and one was stored', () => {
    let storage: FakeSessionStorage;
    let firstId: string;
    let idAfterClearing: string;

    beforeEach(() => {
        storage = new FakeSessionStorage();
        vi.stubGlobal('sessionStorage', storage);

        firstId = getOrCreateRegistrationId().toString();
        clearRegistrationOperation();
        idAfterClearing = getOrCreateRegistrationId().toString();
    });

    afterEach(() => vi.unstubAllGlobals());

    it('should start a different registration, for deliberate reinitiation', () => idAfterClearing.should.not.equal(firstId));
});
