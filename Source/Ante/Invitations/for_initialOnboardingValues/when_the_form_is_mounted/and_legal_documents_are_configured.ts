// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { Guid } from '@cratis/fundamentals';
import { initialRegistrationValues, initialOrganizationSetupValues, initialUserSetupValues } from '../../initialOnboardingValues';

const id = Guid.parse('ff6c55e9-f696-4f9a-a17b-03115977b70a');

// The form applies its initial values after the terms step has set the presented version; seeding one would overwrite it.
describe('when the form is mounted and legal documents are configured', () => {
    it('should leave the join form\'s legal version to the terms step', () => initialUserSetupValues(id, true).should.not.have.property('acceptedLegalVersion'));
    it('should leave the organization setup form\'s legal version to the terms step', () => initialOrganizationSetupValues(id, true).should.not.have.property('acceptedLegalVersion'));
    it('should leave the registration form\'s legal version to the terms step', () => initialRegistrationValues(id, true).should.not.have.property('acceptedLegalVersion'));
});
