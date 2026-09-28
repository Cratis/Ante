// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { CommandResult } from '@cratis/arc/commands';
import type { ICommandResult } from '@cratis/arc/commands';
import { ValidationResult, ValidationResultSeverity } from '@cratis/arc/validation';
import { withOrganizationNameError } from '../../organizationNameCommandResult';

const error = 'Name already in use';
const firstNameError = new ValidationResult(ValidationResultSeverity.Error, 'First name is required', ['firstName'], null);

describe('when the name pre-flight error is merged into command form results', () => {
    let result: ICommandResult<unknown>;

    beforeEach(() => {
        const clientResult = CommandResult.validationFailed([firstNameError]);
        result = withOrganizationNameError(clientResult, undefined, error)!;
    });

    it('should expose the name error once to the wrapped field', () =>
        result.validationResults.filter(validation => validation.members.includes('organizationName')).should.have.lengthOf(1));

    it('should keep errors on other steps', () =>
        result.validationResults.some(validation => validation.members.includes('firstName')).should.be.true);

    it('should not add a second copy of an error already returned by Arc', () => {
        const existing = CommandResult.validationFailed([
            new ValidationResult(ValidationResultSeverity.Error, error, ['organizationName'], null)
        ]);
        (withOrganizationNameError(existing, undefined, error) === existing).should.be.true;
    });

    it('should restore the name error if validation of another field replaces the form result', () => {
        const overwritten = CommandResult.validationFailed([firstNameError]);
        const restored = withOrganizationNameError(overwritten, error, error)!;
        restored.validationResults.filter(validation => validation.members.includes('organizationName')).should.have.lengthOf(1);
    });

    it('should remove only its own error when the name changes', () => {
        const updated = withOrganizationNameError(result, error, undefined)!;
        updated.validationResults.should.have.lengthOf(1);
        updated.validationResults[0].members.should.include('firstName');
    });
});
