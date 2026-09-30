// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// @vitest-environment jsdom

import React, { act } from 'react';
import { createRoot, Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, it } from 'vitest';
import { CommandForm } from '@cratis/arc.react/commands';
import { InputTextField } from '@cratis/components/CommandForm';
import { AcceptInvitation } from '../UserSetup/UserSetup';
import { InitialNameErrors } from '../InitialNameErrors';
import { FieldError } from '../../Accessibility/FieldError';
import { validateChangedName } from '../NameFieldValidation';

Object.assign(globalThis, { IS_REACT_ACT_ENVIRONMENT: true });

describe('when a required name is missing', () => {
    let root: Root;
    let container: HTMLDivElement;
    let input: HTMLInputElement;
    let description: Element;
    let exposedCopies: Element[];
    const message = 'First Name is required.';

    beforeEach(async () => {
        container = document.createElement('div');
        document.body.append(container);
        root = createRoot(container);
        await act(async () => {
            root.render(React.createElement(CommandForm<AcceptInvitation>, {
                command: AcceptInvitation,
                validateOnInit: true,
                onFieldValidate: validateChangedName,
                errorDisplayComponent: FieldError
            },
                React.createElement(InitialNameErrors),
                React.createElement(InputTextField<AcceptInvitation>, { value: c => c.firstName, title: 'First name' })));
        });
        input = container.querySelector('input')!;
        description = container.querySelector(`#${input.getAttribute('aria-describedby')}`)!;
        exposedCopies = [...container.querySelectorAll('*')].filter(element =>
            element.children.length === 0 && element.textContent!.includes(message) && !element.closest('[aria-hidden="true"]'));
    });

    afterEach(async () => {
        await act(async () => root.unmount());
        container.remove();
    });

    it('should mark the field invalid', () => input.getAttribute('aria-invalid')!.should.equal('true'));
    it('should describe the field by its own error element', () => description.textContent!.should.contain(message));
    it('should leave the error element the only place the message is exposed', () => exposedCopies.should.deep.equal([description]));
    it('should still show the message on screen', () => container.querySelector('small.ante-field-error')!.textContent!.should.equal(message));
    it('should have no alert or live region', () => container.querySelectorAll('[role="alert"], [role="status"], [aria-live]').length.should.equal(0));
});
