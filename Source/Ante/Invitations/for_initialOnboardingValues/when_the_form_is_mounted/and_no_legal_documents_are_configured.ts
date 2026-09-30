// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { ICommandResult } from '@cratis/arc/commands';
import { Guid } from '@cratis/fundamentals';
import { RegisterOrganization } from '../../../Organization/Registration/Registration';
import { SetupOrganization } from '../../OrganizationSetup/OrganizationSetup';
import { AcceptInvitation } from '../../UserSetup/UserSetup';
import { initialRegistrationValues, initialOrganizationSetupValues, initialUserSetupValues } from '../../initialOnboardingValues';

type ClientSideValidation = { validateClientSide(): ICommandResult<unknown> };
const id = Guid.parse('ff6c55e9-f696-4f9a-a17b-03115977b70a');

// Without legal documents no terms step sets a version, so the seeded values alone must satisfy the
// command's required properties once the person has filled in the fields they can see.
const validAfterFillingIn = (command: object, initialValues: object, entered: object) => {
    Object.assign(command, initialValues, entered);
    return (command as ClientSideValidation).validateClientSide().isValid;
};

describe('when the form is mounted and no legal documents are configured', () => {
    it('should let the join form be submitted once the names are filled in', () =>
        validAfterFillingIn(new AcceptInvitation(), initialUserSetupValues(id, false), { firstName: 'Ada', lastName: 'Lovelace' }).should.be.true);

    it('should let the organization setup form be submitted once the fields are filled in', () =>
        validAfterFillingIn(new SetupOrganization(), initialOrganizationSetupValues(id, false), { organizationName: 'Acme', firstName: 'Ada', lastName: 'Lovelace' }).should.be.true);

    it('should let the registration form be submitted once the fields are filled in', () =>
        validAfterFillingIn(new RegisterOrganization(), initialRegistrationValues(id, false), { organizationName: 'Acme', firstName: 'Ada', lastName: 'Lovelace' }).should.be.true);
});
