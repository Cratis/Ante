// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import sinon from 'sinon';
import { ValidationResult, ValidationResultReason, ValidationResultSeverity } from '@cratis/arc/validation';
import { registrationValidationFailure } from '../../registrationValidationFailure';

describe('when the stepper reports a validation failure for an earlier submission', () => {
    const markSubmitted = sinon.spy();

    beforeEach(() => {
        markSubmitted.resetHistory();
        const stepperProps = registrationValidationFailure(markSubmitted);
        stepperProps.onValidationFailure?.([
            new ValidationResult(ValidationResultSeverity.Error, 'The wording can change', [], 'OneUseOnboardingAttempt', ValidationResultReason.Rule)
        ]);
    });

    it('should resume status polling through the stepper validation callback', () =>
        markSubmitted.calledOnce.should.be.true);
});
