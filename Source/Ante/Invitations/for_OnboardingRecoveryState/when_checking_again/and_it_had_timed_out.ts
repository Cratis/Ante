// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { OnboardingRecoveryState } from '../../OnboardingRecoveryState';
import { FakeScheduler } from '../FakeScheduler';

describe('when checking again and it had timed out', () => {
    let state: OnboardingRecoveryState;
    let scheduler: FakeScheduler;

    beforeEach(() => {
        scheduler = new FakeScheduler();
        state = new OnboardingRecoveryState(() => {}, 20000, undefined, scheduler);
        state.markSubmitted();
        scheduler.fire();

        state.checkAgain();
    });

    it('should go back to waiting rather than the form', () => state.phase.should.equal('waiting'));
    it('should not create a new operation - it remains waiting, never form', () => state.isWaiting.should.be.true);
    it('should rearm the timeout window', () => scheduler.isScheduled.should.be.true);
});
