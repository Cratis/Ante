// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it, vi } from 'vitest';
import { getOrCreateRegistrationOperation } from '../RegistrationOperation';

describe('when recovering a registration operation after a reload', () => {
    let firstId: string;
    let recoveredId: string;
    let wasRecovered: boolean;

    beforeEach(() => {
        const storage = new Map<string, string>();
        vi.stubGlobal('sessionStorage', {
            getItem: (key: string) => storage.get(key) ?? null,
            setItem: (key: string, value: string) => { storage.set(key, value); },
        });
        const first = getOrCreateRegistrationOperation();
        first.isRecovered.should.be.false;
        firstId = first.id.toString();
        const recovered = getOrCreateRegistrationOperation();
        recoveredId = recovered.id.toString();
        wasRecovered = recovered.isRecovered;
        vi.unstubAllGlobals();
    });

    it('should reuse the persisted id', () => recoveredId.should.equal(firstId));
    it('should enable recovery polling', () => wasRecovered.should.be.true);
});
