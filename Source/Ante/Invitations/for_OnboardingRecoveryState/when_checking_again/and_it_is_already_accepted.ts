// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { OnboardingRecoveryState } from '../../OnboardingRecoveryState';
import { FakeScheduler } from '../FakeScheduler';

describe('when checking again and it is already accepted', () => {
    let state: OnboardingRecoveryState;
    let scheduler: FakeScheduler;

    beforeEach(() => {
        scheduler = new FakeScheduler();
        state = new OnboardingRecoveryState(() => {}, 20000, undefined, scheduler);
        state.updateStatus(true, true);

        state.checkAgain();
    });

    it('should remain accepted', () => state.isAccepted.should.be.true);
    it('should not arm a timeout window', () => scheduler.isScheduled.should.be.false);
});
