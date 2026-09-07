// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { OnboardingRecoveryState } from '../../OnboardingRecoveryState';
import { FakeScheduler } from '../FakeScheduler';

describe('when updating status and acceptance arrives after timing out', () => {
    let state: OnboardingRecoveryState;
    let scheduler: FakeScheduler;

    beforeEach(() => {
        scheduler = new FakeScheduler();
        state = new OnboardingRecoveryState(() => {}, 20000, undefined, scheduler);
        state.markSubmitted();
        scheduler.fire();

        // A late status read - possibly from a poll that started before the timeout fired - confirms the
        // flow actually did publish. Timing out never claimed failure, so late success must still win.
        state.updateStatus(true, true);
    });

    it('should leave the timed-out state and resume waiting for the redirect', () => state.phase.should.equal('waiting'));
    it('should be accepted', () => state.isAccepted.should.be.true);
});
