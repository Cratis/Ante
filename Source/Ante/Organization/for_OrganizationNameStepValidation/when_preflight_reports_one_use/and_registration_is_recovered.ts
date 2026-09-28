// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import { CommandResult } from '@cratis/arc/commands';
import { ValidationResult, ValidationResultReason, ValidationResultSeverity } from '@cratis/arc/validation';
import { OrganizationNameStepValidation } from '../../OrganizationNameStepValidation';
import type { OrganizationNameCommand } from '../../OrganizationNameCommand';
import { shouldResumeRegistrationAfterFailure } from '../../Registration/shouldResumeRegistrationAfterFailure';

class RegistrationCommand implements OrganizationNameCommand {
    organizationName = '';
    firstName = '';
    lastName = '';
    acceptedLegalTerms = false;
    acceptedLegalVersion = '';
    validateClientSide() { return CommandResult.empty; }
    async validate() {
        return CommandResult.validationFailed([
            new ValidationResult(ValidationResultSeverity.Error, 'Already submitted', [], 'OneUseOnboardingAttempt', ValidationResultReason.Rule)
        ]);
    }
}

describe('when name pre-flight finds an earlier registration submission', () => {
    let resumes: number;
    let validation: OrganizationNameStepValidation<RegistrationCommand>;

    beforeEach(async () => {
        vi.useFakeTimers();
        resumes = 0;
        validation = new OrganizationNameStepValidation(
            () => new RegistrationCommand(), () => 'Could not validate',
            results => { if (shouldResumeRegistrationAfterFailure(results)) resumes++; }
        );
        const command = new RegistrationCommand();
        command.organizationName = 'Acme';
        validation.onFieldChange(command, 'organizationName', '', 'Acme');
        await vi.runAllTimersAsync();
    });

    afterEach(() => vi.useRealTimers());

    it('should resume polling rather than treating a one-use failure as a fresh registration', () =>
        resumes.should.equal(1));
});
