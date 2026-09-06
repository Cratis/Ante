// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import React from 'react';
import { StepperPanel } from '@cratis/components/CommandDialog';
import { isCommandFormField } from '@cratis/arc.react/commands';
import { LegalAcceptanceField } from '../LegalAcceptanceField';
import { LegalDocumentKind } from '../LegalDocumentKind';

interface AcceptanceTestCommand {
    acceptedLegalTerms?: boolean;
}

// Regression coverage for the invite-flow bug where the terms-and-privacy checkbox never registered as
// checked and submitting always failed. CommandForm and CommandStepper both discover a command-bound field
// by walking the JSX children they were given AS AUTHORED - React.Children.toArray(props.children) - and
// bind only the ones whose type passes isCommandFormField. They never invoke a plain wrapper component to
// see what it renders, so a field nested inside one is invisible to that walk: it renders, but is never
// wrapped with the CommandFormFieldWrapper that supplies currentValue/onValueChange, so it is stuck showing
// its defaultValue (false) and its onChange is a no-op. Every page composes LegalAcceptanceField as a direct
// StepperPanel child instead, which this spec locks in.
describe('when LegalAcceptanceField is composed directly inside a StepperPanel', () => {
    const stepperPanel = React.createElement(
        StepperPanel,
        { header: 'Terms' },
        React.createElement(LegalAcceptanceField<AcceptanceTestCommand>, {
            value: (command: AcceptanceTestCommand) => command.acceptedLegalTerms,
            onShowDocument: (_kind: LegalDocumentKind) => { }
        }));

    const directChild = (stepperPanel.props as { children: React.ReactElement }).children;

    it('should be discoverable as a command-bound field by the same check CommandForm and CommandStepper use', () => {
        isCommandFormField(directChild.type as never).should.be.true;
    });
});
