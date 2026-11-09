// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// @vitest-environment jsdom

import React, { act } from 'react';
import { createRoot, Root } from 'react-dom/client';
import sinon from 'sinon';
import { afterEach, beforeEach, describe, it } from 'vitest';
import { Guid } from '@cratis/fundamentals';
import { ForAttempt } from '../HostOutcome/HostOutcome';
import { ForOperation } from '../../Onboarding/Provisioning/HostProvisioningOutcomes';
import { HostOutcomeStatus } from '../HostOutcome/HostOutcomeStatus';
import { HostProvisioningStatus } from '../../Onboarding/Provisioning/HostProvisioningStatus';
import { HOST_OUTCOME_GIVE_UP_AFTER_MS, HOST_OUTCOME_POLL_INTERVAL_MS, useHostOutcome } from '../useHostOutcome';
import { resolveHostOutcomeGate } from '../HostOutcomeGate';

Object.assign(globalThis, { IS_REACT_ACT_ENVIRONMENT: true });

describe('when the durable lookup fails despite advisory success', () => {
    const operationId = Guid.parse('3f1d7c2e-5b0a-4d8e-9f6a-1c2b3d4e5f60');
    let root: Root;
    let container: HTMLDivElement;
    let clock: sinon.SinonFakeTimers;
    let latest: ReturnType<typeof useHostOutcome>;
    let lookup: sinon.SinonSpy;
    let failed: boolean;
    const Capture = () => { latest = useHostOutcome(operationId); return null; };

    beforeEach(async () => {
        failed = false;
        clock = sinon.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout', 'setInterval', 'clearInterval'] });
        lookup = sinon.spy();
        sinon.stub(ForAttempt, 'use').returns([{
            hasData: true, isSuccess: true, data: { attemptId: operationId, isConfigured: true, status: HostOutcomeStatus.succeeded, reasonCode: '' }
        }, lookup] as unknown as ReturnType<typeof ForAttempt.use>);
        sinon.stub(ForOperation, 'use').callsFake(() => [{
            hasData: true, isSuccess: !failed, isPerforming: false,
            data: failed ? {} : { operationId, isRequired: true, status: HostProvisioningStatus.pending, reasonCode: '' }
        }, lookup] as unknown as ReturnType<typeof ForOperation.use>);
        container = document.createElement('div');
        document.body.append(container);
        root = createRoot(container);
        await act(async () => root.render(React.createElement(Capture)));
        failed = true;
        await act(async () => root.render(React.createElement(Capture)));
    });

    afterEach(async () => {
        await act(async () => root.unmount());
        container.remove();
        clock.restore();
        sinon.restore();
    });

    it('should retain the required gate and suppress advisory success', () => {
        latest.isConfigured.should.be.true;
        latest.isRead.should.be.false;
        latest.status.should.equal(HostOutcomeStatus.pending);
        resolveHostOutcomeGate(true, latest.isConfigured, latest.isRead, latest.status).should.equal('keepWaiting');
    });

    it('should retry the failed lookup automatically', async () => {
        await act(async () => clock.tick(HOST_OUTCOME_POLL_INTERVAL_MS));
        lookup.called.should.be.true;
    });

    it('should offer retry rather than handoff after timeout', async () => {
        await act(async () => clock.tick(HOST_OUTCOME_GIVE_UP_AFTER_MS));
        latest.hasGivenUp.should.be.true;
        latest.status.should.equal(HostOutcomeStatus.pending);
        resolveHostOutcomeGate(true, latest.isConfigured, latest.isRead, latest.status, latest.hasGivenUp).should.equal('showHostOutcome');
        await act(async () => latest.checkAgain());
        latest.hasGivenUp.should.be.false;
    });
});
