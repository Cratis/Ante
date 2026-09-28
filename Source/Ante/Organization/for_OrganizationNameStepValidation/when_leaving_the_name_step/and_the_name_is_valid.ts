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
    validateClientSide() { return CommandResult.empty; }
    async validate() {
        return CommandResult.validationFailed([
            new ValidationResult(ValidationResultSeverity.Error, 'First name is required', ['firstName'], null)
        ]);
    }
}

describe('when leaving the name step with a valid name and incomplete later steps', () => {
    let command: NameCommand;
    let validation: OrganizationNameStepValidation<NameCommand>;

    beforeEach(async () => {
        vi.useFakeTimers();
        command = new NameCommand();
        validation = new OrganizationNameStepValidation(() => new NameCommand(), () => 'Could not validate');
        command.organizationName = 'Acme';
        validation.onFieldChange(command, 'organizationName', '', 'Acme');
        await vi.runAllTimersAsync();
    });

    afterEach(() => vi.useRealTimers());

    it('should release navigation to the next step even when later fields are invalid', () => {
        validation.getSnapshot().isValidating.should.be.false;
        (validation.getSnapshot().error === undefined).should.be.true;
    });

    it('should not change the user-entered name or seed the later step on the wizard', () => {
        command.organizationName.should.equal('Acme');
        command.firstName.should.equal('');
    });
});
