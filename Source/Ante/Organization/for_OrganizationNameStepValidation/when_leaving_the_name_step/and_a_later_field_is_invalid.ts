// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import { CommandResult } from '@cratis/arc/commands';
import { ValidationResult, ValidationResultSeverity } from '@cratis/arc/validation';
import { OrganizationNameStepValidation } from '../../OrganizationNameStepValidation';
import type { OrganizationNameCommand } from '../../OrganizationNameCommand';

const laterFieldError = new ValidationResult(ValidationResultSeverity.Error, 'First name is too long', ['firstName'], null);
const nameError = new ValidationResult(ValidationResultSeverity.Error, 'Name cannot contain spaces', ['organizationName'], null);

class NameCommand implements OrganizationNameCommand {
    organizationName = '';
    firstName = '';
    lastName = '';
    acceptedLegalTerms = false;
    acceptedLegalVersion = '';
    serverValidations = 0;

    validateClientSide() {
        return this.firstName.length > 100 ? CommandResult.validationFailed([laterFieldError]) : CommandResult.empty;
    }

    async validate() {
        const clientResult = this.validateClientSide();
        if (!clientResult.isValid) return clientResult; // Arc skips the server when any client field fails.
        this.serverValidations++;
        return CommandResult.validationFailed([nameError]);
    }
}

describe('when leaving the name step after a later field becomes invalid client-side', () => {
    let command: NameCommand;
    let probe: NameCommand;
    let validation: OrganizationNameStepValidation<NameCommand>;

    beforeEach(async () => {
        vi.useFakeTimers();
        command = new NameCommand();
        command.firstName = 'x'.repeat(101);
        probe = new NameCommand();
        validation = new OrganizationNameStepValidation(() => probe, 'Could not validate');
        command.organizationName = 'Bad Name';
        validation.onFieldChange(command, 'organizationName', '', 'Bad Name');
        await vi.runAllTimersAsync();
    });

    afterEach(() => vi.useRealTimers());

    it('should still run the server name rules on a name-only probe', () =>
        probe.serverValidations.should.equal(1));

    it('should block leaving the name step with its server error', () =>
        validation.getSnapshot().error!.should.equal(nameError.message));

    it('should leave the invalid later field untouched on the wizard command', () =>
        command.firstName.should.have.lengthOf(101));
});
