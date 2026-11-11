// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { InputTextField } from '@cratis/components/CommandForm';
import { CommandStepper, StepperPanel, type CommandStepperProps } from '@cratis/components/CommandDialog';
import { AcceptInvitation } from './UserSetup';
import { LegalAcceptanceField } from '../../Legal/LegalAcceptanceField';
import { LegalDocumentKind } from '../../Legal/LegalDocumentKind';
import { LegalVersionValues } from '../../Legal/LegalVersionValues';
import { FieldError } from '../../Accessibility/FieldError';
import { InitialNameErrors } from '../InitialNameErrors';
import { validateChangedName } from '../NameFieldValidation';
import strings from 'Strings';

type UserSetupWizardProps = {
    /** The invitation being accepted; a new invitation restarts the stepper. */
    invitationKey: string;
    /** The command's initial values. */
    initialValues: CommandStepperProps<AcceptInvitation>['initialValues'];
    /** The legal terms step is shown only for configured documents, with this version. */
    legal: { isConfigured: boolean; version: string };
    /** Opens a legal document. */
    onShowDocument: (kind: LegalDocumentKind) => void;
    /** Called once the command succeeded. */
    onSuccess: () => Promise<void>;
    /** Called whenever a field changes. */
    onFieldChange: () => void;
    /** Called when the server rejects the command. */
    onValidationFailure: CommandStepperProps<AcceptInvitation>['onValidationFailure'];
};

/**
 * The user setup wizard: the user information step and, when legal documents are configured, the terms step.
 * @param props The command wiring and the legal document settings.
 * @returns The wizard.
 */
export const UserSetupWizard = ({ invitationKey, initialValues, legal, onShowDocument, onSuccess, onFieldChange, onValidationFailure }: UserSetupWizardProps) => (
    <CommandStepper<AcceptInvitation>
        key={invitationKey}
        command={AcceptInvitation}
        validateOnInit
        errorDisplayComponent={FieldError}
        onFieldValidate={validateChangedName}
        onFieldChange={onFieldChange}
        okLabel={strings.userSetup.acceptInvitation}
        initialValues={initialValues}
        onSuccess={onSuccess}
        onValidationFailure={onValidationFailure}
    >
        {/* Every child here has to be a StepperPanel. A component that renders one is not one:
            the stepper counts it as a step but never displays it, which would push the terms step
            into the position the wizard treats as the last one — so Next would submit the
            invitation with the terms still unaccepted instead of showing them. */}
        <StepperPanel header={strings.userSetup.stepUserInformation}>
            <InitialNameErrors />
            <InputTextField<AcceptInvitation>
                value={c => c.firstName}
                title={strings.userSetup.firstName}
                placeholder={strings.userSetup.firstNamePlaceholder}
                pt={{ root: { autoComplete: 'given-name' } }}
            />
            <InputTextField<AcceptInvitation>
                value={c => c.middleName}
                title={strings.userSetup.middleName}
                placeholder={strings.userSetup.middleNamePlaceholder}
                pt={{ root: { autoComplete: 'additional-name' } }}
            />
            <InputTextField<AcceptInvitation>
                value={c => c.lastName}
                title={strings.userSetup.lastName}
                placeholder={strings.userSetup.lastNamePlaceholder}
                pt={{ root: { autoComplete: 'family-name' } }}
            />
        </StepperPanel>
        {legal.isConfigured && (
            <StepperPanel header={strings.userSetup.stepTermsConditions}>
                <LegalVersionValues version={legal.version} />
                <LegalAcceptanceField<AcceptInvitation> value={c => c.acceptedLegalTerms} onShowDocument={onShowDocument} />
            </StepperPanel>
        )}
    </CommandStepper>
);
