// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { OnboardingRecoveryState } from '../../OnboardingRecoveryState';
import { FakeScheduler } from '../FakeScheduler';

describe('when the timeout elapses and still not accepted', () => {
    let state: OnboardingRecoveryState;
    let scheduler: FakeScheduler;

    beforeEach(() => {
        scheduler = new FakeScheduler();
        state = new OnboardingRecoveryState(() => {}, 20000, undefined, scheduler);
        state.markSubmitted();
        scheduler.fire();
    });

    it('should show a recoverable timed-out state, not the form', () => state.phase.should.equal('timedOut'));
    it('should not claim acceptance', () => state.isAccepted.should.be.false);
});
