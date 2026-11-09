// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@cratis/fundamentals';
import { QueryResultWithState } from '@cratis/arc/queries';
import { combineHostOutcomes, hasReadHostProvisioning } from '../durableHostOutcome';
import { HostOutcomeStatus } from '../HostOutcome/HostOutcomeStatus';
import { HostProvisioningStatus } from '../../Onboarding/Provisioning/HostProvisioningStatus';
import { ForOperation, HostProvisioningView } from '../../Onboarding/Provisioning/HostProvisioningOutcomes';

const operation = Guid.parse('3f1d7c2e-5b0a-4d8e-9f6a-1c2b3d4e5f60');
const advisory = { isConfigured: true, isRead: true, status: HostOutcomeStatus.succeeded, reasonCode: '' };
const durable = (status: HostProvisioningStatus, reasonCode = '') => ({ isRead: true, isRequired: true, status, reasonCode });

describe('when combining the advisory and the durable host outcome', () => {
    it('should use the advisory outcome unchanged when durable outcomes are not required', () => {
        const combined = combineHostOutcomes(advisory, { isRead: true, isRequired: false, status: HostProvisioningStatus.unknown, reasonCode: '' });
        combined.status.should.equal(HostOutcomeStatus.succeeded);
        combined.requiresNewInvitation.should.be.false;
    });

    it('should not be read until the durable lookup has settled', () =>
        combineHostOutcomes(advisory, { ...durable(HostProvisioningStatus.succeeded), isRead: false }).isRead.should.be.false);

    it('should never let the advisory outcome report success when durable outcomes are required', () =>
        combineHostOutcomes(advisory, durable(HostProvisioningStatus.pending)).status.should.equal(HostOutcomeStatus.pending));

    it('should stay pending for a host that has not answered, however long it takes', () =>
        combineHostOutcomes({ ...advisory, status: HostOutcomeStatus.failed }, durable(HostProvisioningStatus.pending)).status.should.equal(HostOutcomeStatus.pending));

    it('should report a durable success', () =>
        combineHostOutcomes({ ...advisory, status: HostOutcomeStatus.unknown }, durable(HostProvisioningStatus.succeeded)).status.should.equal(HostOutcomeStatus.succeeded));

    it('should report a durable failure with its generic reason code', () => {
        const combined = combineHostOutcomes(advisory, durable(HostProvisioningStatus.failed, 'quota-exceeded'));
        combined.status.should.equal(HostOutcomeStatus.failed);
        combined.reasonCode.should.equal('quota-exceeded');
        combined.requiresNewInvitation.should.be.false;
    });

    it('should report that a new invitation is required', () => {
        const combined = combineHostOutcomes(advisory, durable(HostProvisioningStatus.requiresNewInvitation, 'invitation-expired'));
        combined.status.should.equal(HostOutcomeStatus.failed);
        combined.requiresNewInvitation.should.be.true;
    });
});

describe('when checking whether the durable host outcome has been read', () => {
    const settled = (operationId: Guid) => ({ hasData: true, isSuccess: true, isPerforming: false, data: { operationId, isRequired: true } });

    it('should not count the result Arc starts the query with', () =>
        hasReadHostProvisioning(QueryResultWithState.initial<HostProvisioningView>(new ForOperation().defaultValue), operation).should.be.false);
    it('should not count a previous operation', () => hasReadHostProvisioning(settled(Guid.empty), operation).should.be.false);
    it('should count the lookup for the operation', () => hasReadHostProvisioning(settled(operation), operation).should.be.true);
    it('should count a failed lookup, which reads as not required', () =>
        hasReadHostProvisioning({ hasData: true, isSuccess: false, isPerforming: false, data: {} }, operation).should.be.true);
});
