// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { ValidationResult, ValidationResultSeverity } from '@cratis/arc/validation';
import { unhandledValidationMessages } from '../unhandledValidationMessages';

describe('when the server rejects a name on an editable step', () => {
    it('should leave member-attributed name failures in the form', () => {
        const nameError = new ValidationResult(ValidationResultSeverity.Error, 'Fornavn er påkrevd.', ['FirstName'], null);
        unhandledValidationMessages([nameError]).should.have.lengthOf(0);
    });
    it('should still show a non-field rejection outside the form', () => {
        const rejection = new ValidationResult(ValidationResultSeverity.Error, 'Invitation has expired', [], null);
        unhandledValidationMessages([rejection]).should.deep.equal(['Invitation has expired']);
    });
});
