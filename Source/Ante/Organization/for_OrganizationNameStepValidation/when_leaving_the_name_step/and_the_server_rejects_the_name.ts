// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import { CommandResult } from '@cratis/arc/commands';
import { ValidationResult, ValidationResultSeverity } from '@cratis/arc/validation';
import { OrganizationNameStepValidation } from '../../OrganizationNameStepValidation';
import type { OrganizationNameCommand } from '../../OrganizationNameCommand';

class NameCommand implements OrganizationNameCommand {
    organizationName = '';
    firstName = '';
    lastName = '';
    acceptedLegalTerms = false;
    acceptedLegalVersion = '';
    validations = 0;
    submissions = 0;
    result = CommandResult.empty;

    validateClientSide() { return CommandResult.empty; }
    async validate() { this.validations++; return this.result; }
    async execute() { this.submissions++; return CommandResult.empty; }
}

const nameRejected = CommandResult.validationFailed([
    new ValidationResult(ValidationResultSeverity.Error, 'Name cannot contain spaces', ['organizationName'], null)
]);

const changeName = (validation: OrganizationNameStepValidation<NameCommand>, command: NameCommand, name: string) => {
    const previous = command.organizationName;
    command.organizationName = name;
    validation.onFieldChange(command, 'organizationName', previous, name);
};

describe('when leaving the name step with a server-rejected name', () => {
    let command: NameCommand;
    let probe: NameCommand;
    let validation: OrganizationNameStepValidation<NameCommand>;

    beforeEach(async () => {
        vi.useFakeTimers();
        command = new NameCommand();
        probe = new NameCommand();
        probe.result = nameRejected;
        validation = new OrganizationNameStepValidation(() => probe, 'Could not validate');
        changeName(validation, command, 'Bad Name');
        validation.onFieldChange(command, 'organizationName', 'Bad Name', 'Bad Name', { isValid: true, errors: [] });
        await vi.runAllTimersAsync();
    });

    afterEach(() => vi.useRealTimers());

    it('should expose the server error to the name field on its own step', () =>
        validation.getSnapshot().error!.should.equal('Name cannot contain spaces'));

    it('should retain the name error while fields on later steps change', () => {
        validation.onFieldChange(command, 'firstName', '', 'Jane');
        validation.getSnapshot().error!.should.equal('Name cannot contain spaces');
    });

    it('should pre-flight a separate command without submitting the wizard', () => {
        probe.validations.should.equal(1);
        command.submissions.should.equal(0);
        probe.submissions.should.equal(0);
    });

    it('should fill required later-step fields only on the pre-flight copy', () => {
        probe.firstName.should.equal('Validation');
        probe.lastName.should.equal('Only');
        command.firstName.should.equal('');
    });
});
