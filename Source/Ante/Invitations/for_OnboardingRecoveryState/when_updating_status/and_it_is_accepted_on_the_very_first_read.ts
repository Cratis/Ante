// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { OnboardingRecoveryState } from '../../OnboardingRecoveryState';
import { FakeScheduler } from '../FakeScheduler';

describe('when updating status and it is accepted on the very first read', () => {
    let state: OnboardingRecoveryState;
    let scheduler: FakeScheduler;

    beforeEach(() => {
        scheduler = new FakeScheduler();
        state = new OnboardingRecoveryState(() => {}, 20000, undefined, scheduler);
        // Returning to the wizard long after the flow already fully published - nothing was submitted
        // this session, and there is no in-between "recorded" moment this client ever observed.
        state.updateStatus(true, true);
    });

    it('should render the waiting phase, never the form', () => state.phase.should.equal('waiting'));
    it('should be accepted', () => state.isAccepted.should.be.true);
    it('should not arm a timeout window', () => scheduler.isScheduled.should.be.false);
});
