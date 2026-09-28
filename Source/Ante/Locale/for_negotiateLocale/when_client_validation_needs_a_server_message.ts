// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@cratis/fundamentals';
import { RegisterOrganization } from '../../Organization/Registration/Registration';
import { SetupOrganization } from '../../Invitations/OrganizationSetup/OrganizationSetup';
import { AcceptInvitation } from '../../Invitations/UserSetup/UserSetup';

describe('when a generated client checks a name before server validation', () => {
    it('should not short-circuit localized server responses with build-time English name rules', () => {
        const registration = new RegisterOrganization();
        registration.registrationId = Guid.empty;
        registration.organizationName = '';
        registration.firstName = '';
        registration.lastName = '';
        registration.acceptedLegalTerms = false;
        registration.acceptedLegalVersion = '';
        registration.validateClientSide().validationResults.should.have.lengthOf(0);

        const setup = new SetupOrganization();
        setup.invitationId = Guid.empty;
        setup.organizationName = '';
        setup.firstName = '';
        setup.lastName = '';
        setup.acceptedLegalTerms = false;
        setup.acceptedLegalVersion = '';
        setup.validateClientSide().validationResults.should.have.lengthOf(0);

        const acceptance = new AcceptInvitation();
        acceptance.invitationId = Guid.empty;
        acceptance.firstName = '';
        acceptance.lastName = '';
        acceptance.acceptedLegalTerms = false;
        acceptance.acceptedLegalVersion = '';
        acceptance.validateClientSide().validationResults.should.have.lengthOf(0);
    });
});
