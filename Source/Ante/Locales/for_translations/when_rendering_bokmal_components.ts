// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { afterEach } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { CratisComponentsProvider, type CratisComponentsProviderProps } from '@cratis/components/Common';
import { CommandStepper, StepperPanel } from '@cratis/components/CommandDialog';
import { Dialog, type DialogProps } from '@cratis/components/Dialogs';
import { DialogButtons } from '@cratis/arc.react/dialogs';
import { RegisterOrganization } from '../../Organization/Registration/Registration';
import strings, { selectStrings } from '../Strings';

// createElement accepts children separately, although these providers declare children required in props.
const Provider = CratisComponentsProvider as React.ComponentType<Omit<CratisComponentsProviderProps, 'children'>>;
const RenderDialog = Dialog as React.ComponentType<Omit<DialogProps, 'children'>>;

describe('when rendering Components with the Bokmål messages', () => {
    let html: string;
    let submitHtml: string;

    beforeEach(() => {
        selectStrings('nb-NO');
        html = renderToStaticMarkup(React.createElement(
            Provider,
            { value: { locale: 'nb-NO', messages: strings.components } },
            React.createElement(CommandStepper, { command: RegisterOrganization },
                React.createElement(StepperPanel, { header: 'Organisasjon' }, 'Første trinn'),
                React.createElement(StepperPanel, { header: 'Kontakt' }, 'Andre trinn')),
            React.createElement(RenderDialog, { title: 'Bekreft', buttons: DialogButtons.YesNo }, 'Bekreft valget')
        ));
        submitHtml = renderToStaticMarkup(React.createElement(
            Provider,
            { value: { locale: 'nb-NO', messages: strings.components } },
            React.createElement(CommandStepper, { command: RegisterOrganization },
                React.createElement(StepperPanel, { header: 'Organisasjon' }, 'Første trinn'))
        ));
    });

    afterEach(() => selectStrings('en'));

    it('should show Bokmål wizard navigation', () => html.should.include('Neste'));
    it('should show Bokmål wizard submit', () => submitHtml.should.include('Send inn'));
    it('should show Bokmål dialog actions', () => html.should.include('Ja</span>'));
    it('should name the dialog close button in Bokmål', () => html.should.include('aria-label="Lukk"'));
});
