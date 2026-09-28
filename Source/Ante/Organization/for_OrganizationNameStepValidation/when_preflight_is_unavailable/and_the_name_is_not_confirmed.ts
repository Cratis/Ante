// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import { CommandResult } from '@cratis/arc/commands';
import type { ICommandResult } from '@cratis/arc/commands';
import { OrganizationNameStepValidation } from '../../OrganizationNameStepValidation';
import type { OrganizationNameCommand } from '../../OrganizationNameCommand';

const unavailableMessage = 'We could not check the name. Please try again.';

class NameCommand implements OrganizationNameCommand {
    organizationName = '';
    firstName = '';
    lastName = '';
    acceptedLegalTerms = false;
    acceptedLegalVersion = '';
    response: () => Promise<ICommandResult<unknown>> = async () => CommandResult.empty;
    validateClientSide() { return CommandResult.empty; }
    validate() { return this.response(); }
}

describe('when the name pre-flight is unavailable', () => {
    let probe: NameCommand;
    let validation: OrganizationNameStepValidation<NameCommand>;

    beforeEach(() => {
        vi.useFakeTimers();
        probe = new NameCommand();
        validation = new OrganizationNameStepValidation(() => probe, unavailableMessage);
    });

    afterEach(() => vi.useRealTimers());

    const validateName = async (nameValidation: OrganizationNameStepValidation<NameCommand>) => {
        const command = new NameCommand();
        command.organizationName = 'Acme';
        nameValidation.onFieldChange(command, 'organizationName', '', 'Acme');
        await vi.runAllTimersAsync();
    };

    it('should block Next and explain an exception result', async () => {
        probe.response = async () => CommandResult.failed(['Transport unavailable']);
        await validateName(validation);
        validation.getSnapshot().isValidating.should.be.false;
        validation.getSnapshot().error!.should.equal(unavailableMessage);
    });

    it('should block Next and explain an unauthorized result', async () => {
        probe.response = async () => ({ ...CommandResult.empty, isAuthorized: false, isSuccess: false });
        await validateName(validation);
        validation.getSnapshot().isValidating.should.be.false;
        validation.getSnapshot().error!.should.equal(unavailableMessage);
    });

    it('should block Next and explain a thrown error', async () => {
        probe.response = async () => { throw new Error('Request failed'); };
        await validateName(validation);
        validation.getSnapshot().isValidating.should.be.false;
        validation.getSnapshot().error!.should.equal(unavailableMessage);
    });
});
