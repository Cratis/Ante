// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, vi } from 'vitest';
import { Guid } from '@cratis/fundamentals';
import { getOrCreateRegistrationId } from '../../RegistrationOperation';
import { FakeSessionStorage } from '../FakeSessionStorage';

describe('when getting or creating a registration id and the stored pointer is a different version', () => {
    let storage: FakeSessionStorage;
    let id: Guid;

    beforeEach(() => {
        storage = new FakeSessionStorage();
        storage.setItem('ante.registration.operation', JSON.stringify({ v: 99, id: Guid.create().toString() }));
        vi.stubGlobal('sessionStorage', storage);
        id = getOrCreateRegistrationId();
    });

    afterEach(() => vi.unstubAllGlobals());

    it('should ignore the incompatible pointer and mint a fresh id', () => Guid.isGuid(id.toString()).should.be.true);
});
