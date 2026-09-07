// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { OnboardingRecoveryState } from '../../OnboardingRecoveryState';
import { FakeScheduler } from '../FakeScheduler';

describe('when updating status and it is recorded but not accepted', () => {
    let state: OnboardingRecoveryState;
    let scheduler: FakeScheduler;

    beforeEach(() => {
        scheduler = new FakeScheduler();
        state = new OnboardingRecoveryState(() => {}, 20000, undefined, scheduler);
        // A reload after submitting but before publication reached the outbox - never submitted locally
        // this session, but durable evidence already shows a recorded operation.
        state.updateStatus(true, false);
    });

    it('should switch to the waiting phase rather than showing the form again', () => state.phase.should.equal('waiting'));
    it('should not be accepted', () => state.isAccepted.should.be.false);
    it('should arm the timeout window', () => scheduler.isScheduled.should.be.true);
});
