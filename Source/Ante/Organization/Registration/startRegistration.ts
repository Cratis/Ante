// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@cratis/fundamentals';
import { ValidationResult } from '@cratis/arc/validation';
import { BeginRegistration } from './Start/BeginningRegistration';
import { shouldResumeRegistrationAfterFailure } from './shouldResumeRegistrationAfterFailure';

type RegistrationStartCommand = { registrationId: Guid; execute: () => Promise<{ isSuccess: boolean; validationResults: ValidationResult[] }> };
type RegistrationStartOutcome = 'started' | 'resume' | 'failed';

/** Start an operation before rendering the wizard; a failed command must never unlock submission. */
export const startRegistration = async (
    registrationId: Guid,
    createCommand: () => RegistrationStartCommand = () => new BeginRegistration()
): Promise<RegistrationStartOutcome> => {
    const command = createCommand();
    command.registrationId = registrationId;
    try {
        const result = await command.execute();
        if (result.isSuccess) return 'started';
        return shouldResumeRegistrationAfterFailure(result.validationResults) ? 'resume' : 'failed';
    } catch {
        return 'failed';
    }
};
