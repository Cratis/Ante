// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// @vitest-environment jsdom

import React, { act, useEffect } from 'react';
import { createRoot, Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, it } from 'vitest';
import { PrimeReactProvider } from '@primereact/core/config';
import { CratisComponentsProvider } from '@cratis/components/Common';
import { primeReactUiLibrary } from '@cratis/components.primereact';
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

// The lobby renders its controls through the PrimeReact adapter, not Components' native ones, so the checkbox's
// description and invalid state have to survive that adapter too.
describe('when acceptance is rejected with the PrimeReact adapter', () => {
    let root: Root;
    let container: HTMLDivElement;
    let checkbox: HTMLInputElement;
    let description: Element | null;

    beforeEach(async () => {
        container = document.createElement('div');
        document.body.append(container);
        root = createRoot(container);
        await act(async () => {
            root.render(React.createElement(PrimeReactProvider, { license: 'spec' } as never,
                React.createElement(CratisComponentsProvider, { library: primeReactUiLibrary, rendererSetup: { 'cratis-primereact.license-configured': true } } as never,
                    React.createElement(CommandForm<AcceptInvitation>, {
                        command: AcceptInvitation,
                        errorDisplayComponent: FieldError
                    },
                        React.createElement(RejectAcceptance),
                        React.createElement(LegalAcceptanceField<AcceptInvitation>, { value: c => c.acceptedLegalTerms, onShowDocument: () => { } })))));
        });
        checkbox = container.querySelector('input[type="checkbox"]')!;
        description = container.querySelector(`#${CSS.escape(checkbox.getAttribute('aria-describedby') ?? 'none')}`);
    });

    afterEach(async () => {
        await act(async () => root.unmount());
        container.remove();
    });

    it('should render through the adapter', () => checkbox.className.should.contain('cratis-primereact-choice__input'));
    it('should mark the checkbox invalid', () => checkbox.getAttribute('aria-invalid')!.should.equal('true'));
    it('should describe the checkbox by an element carrying the message', () => description!.textContent!.should.equal(message));
});
