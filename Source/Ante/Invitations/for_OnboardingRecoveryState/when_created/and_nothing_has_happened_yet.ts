// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { OnboardingRecoveryState } from '../../OnboardingRecoveryState';
import { FakeScheduler } from '../FakeScheduler';

describe('when created', () => {
    let state: OnboardingRecoveryState;

    beforeEach(() => {
        state = new OnboardingRecoveryState(() => {}, 20000, undefined, new FakeScheduler());
    });

    it('should render the form', () => state.phase.should.equal('form'));
    it('should not be waiting', () => state.isWaiting.should.be.false);
    it('should not be accepted', () => state.isAccepted.should.be.false);
});
