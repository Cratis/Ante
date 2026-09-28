// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import { CommandResult } from '@cratis/arc/commands';
import type { ICommandResult } from '@cratis/arc/commands';
import { OrganizationNameStepValidation } from '../../OrganizationNameStepValidation';
import type { OrganizationNameCommand } from '../../OrganizationNameCommand';

class NameCommand implements OrganizationNameCommand {
    organizationName = '';
    firstName = '';
    lastName = '';
    acceptedLegalTerms = false;
    acceptedLegalVersion = '';
    validateClientSide() { return CommandResult.empty; }
    validate(): Promise<ICommandResult<unknown>> { return Promise.resolve(CommandResult.failed(['Offline'])); }
}

describe('when switching language after name validation became unavailable', () => {
    let locale: string;
    let validation: OrganizationNameStepValidation<NameCommand>;

    beforeEach(async () => {
        vi.useFakeTimers();
        locale = 'English';
        validation = new OrganizationNameStepValidation(() => new NameCommand(), () => locale);
        const command = new NameCommand();
        command.organizationName = 'Acme';
        validation.onFieldChange(command, 'organizationName', '', 'Acme');
        await vi.runAllTimersAsync();
        locale = 'Norsk';
        validation.onLocaleChange();
        await vi.runAllTimersAsync();
    });

    afterEach(() => { validation.dispose(); vi.useRealTimers(); });

    it('should retry the entered name and show the new language', () => {
        validation.getSnapshot().error!.should.equal('Norsk');
    });
});
