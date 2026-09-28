// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, describe, it, vi } from 'vitest';
import sinon from 'sinon';
import { startRegistrationStatusPolling } from '../registrationStatusPolling';

describe('when the initial status query takes longer than the polling interval', () => {
    afterEach(() => vi.useRealTimers());

    it('should wait until it completes before refreshing', async () => {
        vi.useFakeTimers();
        let initialQueryPerforming = true;
        const refresh = sinon.stub().resolves();
        const stop = startRegistrationStatusPolling(refresh, () => initialQueryPerforming);

        await vi.advanceTimersByTimeAsync(4500);
        refresh.callCount.should.equal(0);
        initialQueryPerforming = false;
        await vi.advanceTimersByTimeAsync(1500);
        refresh.callCount.should.equal(1);
        stop();
    });
});
