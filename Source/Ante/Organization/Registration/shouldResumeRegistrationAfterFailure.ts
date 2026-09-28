// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ValidationResult, ValidationResultReason } from '@cratis/arc/validation';

/** Only a one-use rejection is evidence that the original submission may have been recorded. */
export const shouldResumeRegistrationAfterFailure = (validationResults: ValidationResult[]): boolean =>
    validationResults.some(validation =>
        (validation.reason === ValidationResultReason.ConstraintViolation &&
            (validation.reasonDetail === 'OneUseOnboardingAttempt' || validation.reasonDetail === 'OneUseRegistration')) ||
        (validation.reason === ValidationResultReason.Rule &&
            (validation.state === 'OneUseOnboardingAttempt' || validation.reasonDetail === 'OneUseOnboardingAttempt')));
