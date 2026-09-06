// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { InvitationFlowType } from '../../../Contracts/Invitations/InvitationFlowType';
import { normalizeFlowType } from '../invitationToken';

describe('when normalizing a value', () => {
    it('should pass an already-typed joinTenant value through', () => {
        normalizeFlowType(InvitationFlowType.joinTenant)!.should.equal(InvitationFlowType.joinTenant);
    });

    it('should pass an already-typed createTenant value through', () => {
        normalizeFlowType(InvitationFlowType.createTenant)!.should.equal(InvitationFlowType.createTenant);
    });

    it('should recognize the createTenant name regardless of case', () => {
        normalizeFlowType('CreateTenant')!.should.equal(InvitationFlowType.createTenant);
    });

    it('should recognize the joinTenant name written with separators', () => {
        normalizeFlowType('join-tenant')!.should.equal(InvitationFlowType.joinTenant);
    });

    it('should return null for a number outside the enum range', () => {
        (normalizeFlowType(5) === null).should.be.true;
    });

    it('should return null for an unrecognized string', () => {
        (normalizeFlowType('somethingElse') === null).should.be.true;
    });

    it('should return null for undefined', () => {
        (normalizeFlowType(undefined) === null).should.be.true;
    });
});
