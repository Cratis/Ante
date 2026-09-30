// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResultWithState } from '@cratis/arc/queries';
import { Guid } from '@cratis/fundamentals';
import { StatusForInvitation, UserSetupAcceptanceStatusView } from '../UserSetup/UserSetup';
import { hasReadStatus } from '../hasReadStatus';

const id = Guid.parse('3f1d7c2e-5b0a-4d8e-9f6a-1c2b3d4e5f60');
const other = Guid.parse('9a8b7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d');
const status = (invitationId: Guid, value: number) => ({ hasData: true, data: { invitationId, status: value } });

describe('when checking whether an onboarding status has been read', () => {
    it('should not count the result Arc starts a query with', () =>
        hasReadStatus(QueryResultWithState.initial<UserSetupAcceptanceStatusView>(new StatusForInvitation().defaultValue), id).should.be.false);
    it('should not count a result without data', () => hasReadStatus({ hasData: false, data: undefined }, id).should.be.false);
    it('should not count a status for another invitation', () => hasReadStatus(status(other, 0), id).should.be.false);
    it('should not count the status of the empty lookup made before the invitation is known', () => hasReadStatus(status(Guid.empty, 0), id).should.be.false);
    it('should count a pending status for the invitation', () => hasReadStatus(status(id, 0), id).should.be.true);
    it('should count an accepted status for the invitation', () => hasReadStatus(status(id, 2), id).should.be.true);
    it('should count a status whose identifier arrived as text', () => hasReadStatus({ hasData: true, data: { invitationId: id.toString().toUpperCase(), status: 0 } }, id).should.be.true);
});
