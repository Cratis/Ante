// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import sinon from 'sinon';
import { Guid } from '@cratis/fundamentals';
import { ValidationResult, ValidationResultReason, ValidationResultSeverity } from '@cratis/arc/validation';
import { startRegistration } from '../../startRegistration';

describe('when preparing the wizard and the start reports an earlier submission', () => {
    it('should resume recovery rather than show a failed start', async () => {
        const validationResults = [new ValidationResult(ValidationResultSeverity.Error, 'The wording can change', [], 'OneUseOnboardingAttempt', ValidationResultReason.Rule)];
        const command = { registrationId: Guid.empty, execute: sinon.stub().resolves({ isSuccess: false, validationResults }) };
        (await startRegistration(Guid.create(), () => command)).should.equal('resume');
    });
});
