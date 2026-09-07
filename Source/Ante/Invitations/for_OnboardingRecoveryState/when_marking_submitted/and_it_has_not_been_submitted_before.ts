// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { OnboardingRecoveryState } from '../../OnboardingRecoveryState';
import { FakeScheduler } from '../FakeScheduler';

describe('when marking submitted', () => {
    let state: OnboardingRecoveryState;
    let scheduler: FakeScheduler;
    let onChangeCallCount: number;

    beforeEach(() => {
        scheduler = new FakeScheduler();
        onChangeCallCount = 0;
        state = new OnboardingRecoveryState(() => { onChangeCallCount++; }, 20000, undefined, scheduler);
        state.markSubmitted();
    });

    it('should switch to the waiting phase', () => state.phase.should.equal('waiting'));
    it('should be waiting', () => state.isWaiting.should.be.true);
    it('should arm the timeout window', () => scheduler.isScheduled.should.be.true);
    it('should arm it for the configured timeout', () => scheduler.scheduledDelayMs!.should.equal(20000));
    it('should notify of the change', () => onChangeCallCount.should.equal(1));
});
