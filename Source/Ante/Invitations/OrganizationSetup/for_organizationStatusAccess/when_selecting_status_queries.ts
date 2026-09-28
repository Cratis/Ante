// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@cratis/fundamentals';
import { describe, it } from 'vitest';
import { OrganizationSetupAcceptanceStatus } from '../OrganizationSetupAcceptanceStatus';
import { organizationStatusIds, shouldRecheckRegistrationStatus } from '../organizationStatusAccess';

describe('when selecting onboarding status queries', () => {
    const id = Guid.create();

    it('should only request the registration status with a self-service id', () => {
        const selected = organizationStatusIds(id, true);
        selected.invitationId.toString().should.equal(Guid.empty.toString());
        selected.registrationId.toString().should.equal(id.toString());
    });

    it('should only request invitation status with an invitation id', () => {
        const selected = organizationStatusIds(id, false);
        selected.invitationId.toString().should.equal(id.toString());
        selected.registrationId.toString().should.equal(Guid.empty.toString());
    });

    it('should not recheck a newly created id before the command succeeds', () =>
        shouldRecheckRegistrationStatus(true, false, false, true, OrganizationSetupAcceptanceStatus.pending).should.be.false);

    it('should recheck after a successful submission', () =>
        shouldRecheckRegistrationStatus(true, true, false, true, OrganizationSetupAcceptanceStatus.pending).should.be.true);

    it('should recheck an unknown registration recovered from a persisted id', () =>
        shouldRecheckRegistrationStatus(true, true, false, false).should.be.true);

    it('should stop rechecking after the recovery window expires', () =>
        shouldRecheckRegistrationStatus(true, true, true, false).should.be.false);

    it('should stop rechecking when the registration is published', () =>
        shouldRecheckRegistrationStatus(true, true, false, true, OrganizationSetupAcceptanceStatus.accepted).should.be.false);

    it('should not recheck registration status in invited flows', () =>
        shouldRecheckRegistrationStatus(false, true, false, false).should.be.false);
});
