// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, vi } from 'vitest';
import { Guid } from '@cratis/fundamentals';
import { getOrCreateRegistrationId } from '../../RegistrationOperation';
import { FakeSessionStorage } from '../FakeSessionStorage';

describe('when getting or creating a registration id and none is stored', () => {
    let storage: FakeSessionStorage;
    let id: Guid;

    beforeEach(() => {
        storage = new FakeSessionStorage();
        vi.stubGlobal('sessionStorage', storage);
        id = getOrCreateRegistrationId();
    });

    afterEach(() => vi.unstubAllGlobals());

    it('should create a valid id', () => Guid.isGuid(id.toString()).should.be.true);
    it('should persist it for a later read', () => getOrCreateRegistrationId().toString().should.equal(id.toString()));
    it('should never persist anything beyond the opaque pointer', () => {
        const raw = storage.getItem('ante.registration.operation')!;
        const parsed = JSON.parse(raw) as Record<string, unknown>;
        Object.keys(parsed).sort().should.deep.equal(['id', 'v']);
    });
});
