// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { InputTextField } from '@cratis/components/CommandForm';
import { CommandStepper, StepperPanel, type CommandStepperProps } from '@cratis/components/CommandDialog';
import { RegisterOrganization } from './Registration';
import { registrationValidationFailure } from './registrationValidationFailure';
import { OrganizationNameStepValidation } from '../OrganizationNameStepValidation';
import { OrganizationNameStepError } from '../OrganizationNameStepError';
import { InitialNameErrors } from '../../Invitations/InitialNameErrors';
import { validateChangedName } from '../../Invitations/NameFieldValidation';
import { LegalAcceptanceField } from '../../Legal/LegalAcceptanceField';
import { LegalDocumentKind } from '../../Legal/LegalDocumentKind';
import { LegalVersionValues } from '../../Legal/LegalVersionValues';
import { FieldError } from '../../Accessibility/FieldError';
import strings from 'Strings';

type RegistrationWizardProps = {
    /** The command's initial values. */
    initialValues: CommandStepperProps<RegisterOrganization>['initialValues'];
    /** The organization name validation the stepper reports busy and errors from. */
    nameValidation: OrganizationNameStepValidation<RegisterOrganization>;
    /** Whether the organization name is being validated. */
    isNameValidating: boolean;
    /** The legal terms step is shown only for configured documents, with this version. */
    legal: { isConfigured: boolean; version: string };
    /** Opens a legal document. */
    onShowDocument: (kind: LegalDocumentKind) => void;
    /** Records the organization name at submit time. */
    onOrganizationName: (organizationName: string) => void;
    /** The signup context captured from the page address, sent with the registration. */
    signupContext: RegisterOrganization['signupContext'];
    /** Marks the hand-off submitted: the command succeeded, or the server already has this registration. */
    onSubmitted: () => void;
};

/**
 * The self-service registration wizard: the organization step, the user information step and, when legal
 * documents are configured, the terms step.
 * @param props The command wiring, the name validation, the signup context and the legal document settings.
 * @returns The wizard.
 */
export const RegistrationWizard = ({ initialValues, nameValidation, isNameValidating, legal, onShowDocument, onOrganizationName, signupContext, onSubmitted }: RegistrationWizardProps) => (
    <CommandStepper<RegisterOrganization>
        command={RegisterOrganization}
        validateOnInit
        errorDisplayComponent={FieldError}
        onFieldValidate={validateChangedName}
        isBusy={isNameValidating}
        onFieldChange={nameValidation.onFieldChange}
        okLabel={strings.registration.register}
        initialValues={initialValues}
        onBeforeExecute={(values) => {
            onOrganizationName(values.organizationName ?? '');
            values.signupContext = signupContext;
            return values;
        }}
        onSuccess={async () => { onSubmitted(); }}
        {...registrationValidationFailure(onSubmitted)}
    >
        <StepperPanel header={strings.organizationSetup.stepOrganization}>
            <InitialNameErrors includeOrganization />
            <InputTextField<RegisterOrganization>
                value={c => c.organizationName}
                title={strings.registration.organizationName}
                placeholder={strings.registration.organizationNamePlaceholder}
                pt={{ root: { autoComplete: 'organization' } }}
            />
            <OrganizationNameStepError validation={nameValidation} />
        </StepperPanel>
        <StepperPanel header={strings.organizationSetup.stepUserInformation}>
            <InputTextField<RegisterOrganization>
                value={c => c.firstName}
                title={strings.organizationSetup.firstName}
                placeholder={strings.organizationSetup.firstNamePlaceholder}
                pt={{ root: { autoComplete: 'given-name' } }}
            />
            <InputTextField<RegisterOrganization>
                value={c => c.middleName}
                title={strings.organizationSetup.middleName}
                placeholder={strings.organizationSetup.middleNamePlaceholder}
                pt={{ root: { autoComplete: 'additional-name' } }}
            />
            <InputTextField<RegisterOrganization>
                value={c => c.lastName}
                title={strings.organizationSetup.lastName}
                placeholder={strings.organizationSetup.lastNamePlaceholder}
                pt={{ root: { autoComplete: 'family-name' } }}
            />
        </StepperPanel>
        {legal.isConfigured && (
            <StepperPanel header={strings.organizationSetup.stepTermsConditions}>
                <LegalVersionValues version={legal.version} />
                <LegalAcceptanceField<RegisterOrganization> value={c => c.acceptedLegalTerms} onShowDocument={onShowDocument} />
            </StepperPanel>
        )}
    </CommandStepper>
);
