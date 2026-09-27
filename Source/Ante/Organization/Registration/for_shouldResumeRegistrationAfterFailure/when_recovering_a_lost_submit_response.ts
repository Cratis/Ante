// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { ValidationResult, ValidationResultReason, ValidationResultSeverity } from '@cratis/arc/validation';
import { shouldResumeRegistrationAfterFailure } from '../shouldResumeRegistrationAfterFailure';

const rejected = (reason: ValidationResultReason, reasonDetail?: string, state?: string) =>
    [new ValidationResult(ValidationResultSeverity.Error, 'The wording can change', [], state, reason, reasonDetail)];

describe('when retrying a registration after a lost response', () => {
    it('should resume after the validator recognizes an earlier submission', () =>
        shouldResumeRegistrationAfterFailure(rejected(ValidationResultReason.Rule, undefined, 'OneUseOnboardingAttempt')).should.be.true);

    it('should resume after the handler recognizes an earlier submission', () =>
        shouldResumeRegistrationAfterFailure(rejected(ValidationResultReason.Rule, 'OneUseOnboardingAttempt')).should.be.true);

    it('should resume after the append-time one-use constraint rejects a race', () =>
        shouldResumeRegistrationAfterFailure(rejected(ValidationResultReason.ConstraintViolation, 'OneUseRegistration')).should.be.true);

    it('should not resume after another validation rule rejects the command', () =>
        shouldResumeRegistrationAfterFailure(rejected(ValidationResultReason.Rule, undefined, 'OtherRule')).should.be.false);

    it('should not resume after an unrelated constraint rejects the command', () =>
        shouldResumeRegistrationAfterFailure(rejected(ValidationResultReason.ConstraintViolation, 'UniqueOrganizationName')).should.be.false);

    it('should not resume without a one-use validation result', () =>
        shouldResumeRegistrationAfterFailure([]).should.be.false);
});
