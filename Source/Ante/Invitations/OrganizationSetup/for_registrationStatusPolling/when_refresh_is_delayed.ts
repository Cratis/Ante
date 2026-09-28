// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, describe, it, vi } from 'vitest';
import sinon from 'sinon';
import { startRegistrationStatusPolling } from '../registrationStatusPolling';

describe('when a status refresh takes longer than the polling interval', () => {
    afterEach(() => vi.useRealTimers());

    it('should not overlap refreshes', async () => {
        vi.useFakeTimers();
        let complete!: () => void;
        const refresh = sinon.stub().callsFake(() => new Promise<void>(resolve => { complete = resolve; }));
        const stop = startRegistrationStatusPolling(refresh, () => false);

        await vi.advanceTimersByTimeAsync(4500);
        refresh.callCount.should.equal(1);
        complete();
        await vi.advanceTimersByTimeAsync(1500);
        refresh.callCount.should.equal(2);
        stop();
    });
});
