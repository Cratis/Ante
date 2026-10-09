// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { resolveHostOutcomeGate } from '../HostOutcomeGate';
import { HostOutcomeStatus } from '../HostOutcome/HostOutcomeStatus';

describe('when resolving the host outcome gate', () => {
    it('should keep waiting when onboarding has not been accepted yet, regardless of configuration', () => {
        resolveHostOutcomeGate(false, true, true).should.equal('keepWaiting');
        resolveHostOutcomeGate(false, false, true).should.equal('keepWaiting');
    });

    it('should redirect automatically once accepted when no host outcome adapter is configured', () =>
        resolveHostOutcomeGate(true, false, true).should.equal('redirectAutomatically'));

    it('should keep waiting while the host has not reported a result yet', () => {
        resolveHostOutcomeGate(true, true, true).should.equal('keepWaiting');
        resolveHostOutcomeGate(true, true, true, HostOutcomeStatus.unknown).should.equal('keepWaiting');
        resolveHostOutcomeGate(true, true, true, HostOutcomeStatus.pending).should.equal('keepWaiting');
    });

    it('should redirect automatically once the host reports success', () =>
        resolveHostOutcomeGate(true, true, true, HostOutcomeStatus.succeeded).should.equal('redirectAutomatically'));

    it('should show the host outcome when the host reports a failure', () =>
        resolveHostOutcomeGate(true, true, true, HostOutcomeStatus.failed).should.equal('showHostOutcome'));

    it('should show the host outcome with its manual actions once waiting has been given up', () =>
        resolveHostOutcomeGate(true, true, true, HostOutcomeStatus.pending, true).should.equal('showHostOutcome'));

    it('should keep waiting once accepted until the host outcome lookup has settled', () =>
        resolveHostOutcomeGate(true, false, false).should.equal('keepWaiting'));
});
