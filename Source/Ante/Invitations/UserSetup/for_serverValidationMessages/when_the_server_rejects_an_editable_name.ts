// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// @vitest-environment jsdom

import React, { act } from 'react';
import { createRoot } from 'react-dom/client';
import { describe, it } from 'vitest';
import { ErrorSummary } from '../../../Accessibility/ErrorSummary';
import { ValidationResult, ValidationResultSeverity } from '@cratis/arc/validation';
import { serverValidationMessages } from '../serverValidationMessages';

describe('when the server rejects a name while the user is on the terms step', () => {
    it('should announce the name rejection regardless of which step owns the field', () => {
        const nameError = new ValidationResult(ValidationResultSeverity.Error, 'Fornavn er påkrevd.', ['FirstName'], null);
        serverValidationMessages([nameError]).should.deep.equal(['Fornavn er påkrevd.']);
    });
    it('should announce a server-only name rejection alongside the still-active terms step', async () => {
        Object.assign(globalThis, { IS_REACT_ACT_ENVIRONMENT: true });
        const container = document.createElement('div');
        document.body.append(container);
        const root = createRoot(container);
        const rejection = new ValidationResult(ValidationResultSeverity.Error, 'Name already in use', ['FirstName'], null);
        await act(async () => root.render(React.createElement('div', null,
            React.createElement('section', { 'aria-label': 'Terms', 'data-active': true }),
            React.createElement(ErrorSummary, { messages: serverValidationMessages([rejection]) }))));
        container.querySelector('[role="alert"]')!.textContent!.should.include('Name already in use');
        Boolean(container.querySelector('[aria-label="Terms"]')).should.be.true;
        await act(async () => root.unmount());
        container.remove();
    });
    it('should also show non-field rejections', () => {
        const rejection = new ValidationResult(ValidationResultSeverity.Error, 'Invitation has expired', [], null);
        serverValidationMessages([rejection]).should.deep.equal(['Invitation has expired']);
    });
});
