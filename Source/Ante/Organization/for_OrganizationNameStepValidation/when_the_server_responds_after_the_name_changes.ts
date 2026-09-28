// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import { CommandResult } from '@cratis/arc/commands';
import { ValidationResult, ValidationResultSeverity } from '@cratis/arc/validation';
import { OrganizationNameStepValidation } from '../OrganizationNameStepValidation';
import type { OrganizationNameCommand } from '../OrganizationNameCommand';

class NameCommand implements OrganizationNameCommand {
    organizationName = '';
    firstName = '';
    lastName = '';
    acceptedLegalTerms = false;
    acceptedLegalVersion = '';
    validateClientSide() { return CommandResult.empty; }
    async validate() { return CommandResult.empty; }
}

describe('when a server rejection arrives after the user has corrected the name', () => {
    let command: NameCommand;
    let validation: OrganizationNameStepValidation<NameCommand>;
    let respond: (result: CommandResult) => void;

    beforeEach(async () => {
        vi.useFakeTimers();
        command = new NameCommand();
        validation = new OrganizationNameStepValidation(() => {
            const probe = new NameCommand();
            probe.validate = () => new Promise(resolve => { respond = resolve; });
            return probe;
        }, () => 'Could not validate');
        command.organizationName = 'Bad Name';
        validation.onFieldChange(command, 'organizationName', '', 'Bad Name');
        await vi.runAllTimersAsync();
        command.organizationName = 'GoodName';
        validation.onFieldChange(command, 'organizationName', 'Bad Name', 'GoodName');
    });

    afterEach(() => vi.useRealTimers());

    it('should keep the next step blocked until validation of the corrected name finishes', () =>
        validation.getSnapshot().isValidating.should.be.true);

    it('should ignore the stale rejection and let a valid name advance', async () => {
        respond!(CommandResult.validationFailed([
            new ValidationResult(ValidationResultSeverity.Error, 'Invalid name', ['organizationName'], null)
        ]));
        await vi.runAllTimersAsync();
        (validation.getSnapshot().error === undefined).should.be.true;
        validation.getSnapshot().isValidating.should.be.true;
        respond!(CommandResult.empty);
        await vi.runAllTimersAsync();
        validation.getSnapshot().isValidating.should.be.false;
    });
});
