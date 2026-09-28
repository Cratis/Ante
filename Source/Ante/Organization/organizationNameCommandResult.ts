// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult } from '@cratis/arc/commands';
import type { ICommandResult } from '@cratis/arc/commands';
import { ValidationResult, ValidationResultSeverity } from '@cratis/arc/validation';

const belongsToName = (result: ValidationResult): boolean =>
    result.members.some(member => member.toLowerCase() === 'organizationname');

/** Merge the pre-flight error into Arc's result without losing errors from other wizard steps. */
export const withOrganizationNameError = (
    commandResult: ICommandResult<unknown> | undefined,
    previousError: string | undefined,
    error: string | undefined
): ICommandResult<unknown> | undefined => {
    if (!previousError && !error) return commandResult;
    const result = commandResult ?? CommandResult.empty;
    const hasNewError = result.validationResults.some(validation => belongsToName(validation) && validation.message === error);
    if (previousError === error && (!error || hasNewError)) return commandResult;

    const otherErrors = result.validationResults.filter(validation =>
        !(previousError && belongsToName(validation) && validation.message === previousError));
    if (error && !otherErrors.some(validation => belongsToName(validation) && validation.message === error)) {
        otherErrors.push(new ValidationResult(ValidationResultSeverity.Error, error, ['organizationName'], null));
    }
    if (otherErrors.length === result.validationResults.length &&
        otherErrors.every((validation, index) => validation === result.validationResults[index])) return commandResult;

    const isValid = otherErrors.length === 0;
    return {
        ...result,
        validationResults: otherErrors,
        isValid,
        isSuccess: result.isAuthorized && isValid && !result.hasExceptions
    };
};
