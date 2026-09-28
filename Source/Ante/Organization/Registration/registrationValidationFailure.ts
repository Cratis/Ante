// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CommandStepperProps } from '@cratis/components/CommandDialog';
import type { RegisterOrganization } from './Registration';
import { shouldResumeRegistrationAfterFailure } from './shouldResumeRegistrationAfterFailure';

/** Wire the stepper's validation callback, rather than its non-validation failure callback, to recovery. */
export const registrationValidationFailure = (markSubmitted: () => void): Pick<CommandStepperProps<RegisterOrganization>, 'onValidationFailure'> => ({
    onValidationFailure: validationResults => {
        if (shouldResumeRegistrationAfterFailure(validationResults)) markSubmitted();
    }
});
