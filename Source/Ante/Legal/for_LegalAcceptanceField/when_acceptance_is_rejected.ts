// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// @vitest-environment jsdom

import React, { act, useEffect } from 'react';
import { createRoot, Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, it } from 'vitest';
import { CommandForm, useCommandFormContext } from '@cratis/arc.react/commands';
import { AcceptInvitation } from '../../Invitations/UserSetup/UserSetup';
import { FieldError } from '../../Accessibility/FieldError';
import { LegalAcceptanceField } from '../LegalAcceptanceField';

Object.assign(globalThis, { IS_REACT_ACT_ENVIRONMENT: true });

const message = 'You must accept the terms and conditions.';

const RejectAcceptance = () => {
    const { setCustomFieldError } = useCommandFormContext();
    useEffect(() => setCustomFieldError('acceptedLegalTerms', message), [setCustomFieldError]);
    return null;
};

describe('when acceptance is rejected', () => {
    let root: Root;
    let container: HTMLDivElement;
    let checkbox: HTMLInputElement;
    let description: Element | null;
    let exposedCopies: Element[];

    beforeEach(async () => {
        container = document.createElement('div');
        document.body.append(container);
        root = createRoot(container);
        await act(async () => {
            root.render(React.createElement(CommandForm<AcceptInvitation>, {
                command: AcceptInvitation,
                errorDisplayComponent: FieldError
            },
                React.createElement(RejectAcceptance),
                React.createElement(LegalAcceptanceField<AcceptInvitation>, { value: c => c.acceptedLegalTerms, onShowDocument: () => { } })));
        });
        checkbox = container.querySelector('input[type="checkbox"]')!;
        description = container.querySelector(`#${CSS.escape(checkbox.getAttribute('aria-describedby') ?? 'none')}`);
        exposedCopies = [...container.querySelectorAll('*')].filter(element =>
            element.children.length === 0 && element.textContent!.includes(message) && !element.closest('[aria-hidden="true"]'));
    });

    afterEach(async () => {
        await act(async () => root.unmount());
        container.remove();
    });

    it('should mark the checkbox invalid', () => checkbox.getAttribute('aria-invalid')!.should.equal('true'));
    it('should describe the checkbox by an element carrying the message', () => description!.textContent!.should.equal(message));
    it('should keep that element out of sight', () => description!.classList.contains('ante-visually-hidden').should.be.true);
    it('should leave that element the only place the message is exposed', () => exposedCopies.should.deep.equal([description]));
    it('should still show the message on screen', () => container.querySelector('small.ante-field-error')!.textContent!.should.equal(message));
});

describe('when acceptance has not been rejected', () => {
    let root: Root;
    let container: HTMLDivElement;
    let checkbox: HTMLInputElement;

    beforeEach(async () => {
        container = document.createElement('div');
        document.body.append(container);
        root = createRoot(container);
        await act(async () => {
            root.render(React.createElement(CommandForm<AcceptInvitation>, {
                command: AcceptInvitation,
                errorDisplayComponent: FieldError
            },
                React.createElement(LegalAcceptanceField<AcceptInvitation>, { value: c => c.acceptedLegalTerms, onShowDocument: () => { } })));
        });
        checkbox = container.querySelector('input[type="checkbox"]')!;
    });

    afterEach(async () => {
        await act(async () => root.unmount());
        container.remove();
    });

    it('should not describe the checkbox', () => (checkbox.getAttribute('aria-describedby') === null).should.be.true);
    it('should not mark the checkbox invalid', () => (checkbox.getAttribute('aria-invalid') === null).should.be.true);
});
