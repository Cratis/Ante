// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { InputTextField } from '@cratis/components/CommandForm';
import { CommandStepper, StepperPanel, type CommandStepperProps } from '@cratis/components/CommandDialog';
import { SetupOrganization } from './OrganizationSetup';
import { OrganizationNameStepValidation } from '../../Organization/OrganizationNameStepValidation';
import { OrganizationNameStepError } from '../../Organization/OrganizationNameStepError';
import { InitialNameErrors } from '../InitialNameErrors';
import { validateChangedName } from '../NameFieldValidation';
import { LegalAcceptanceField } from '../../Legal/LegalAcceptanceField';
import { LegalDocumentKind } from '../../Legal/LegalDocumentKind';
import { LegalVersionValues } from '../../Legal/LegalVersionValues';
import { FieldError } from '../../Accessibility/FieldError';
import strings from 'Strings';

type OrganizationSetupWizardProps = {
    /** The invitation being set up; a new invitation restarts the stepper. */
    invitationKey: string;
    /** The command's initial values. */
    initialValues: CommandStepperProps<SetupOrganization>['initialValues'];
    /** The organization name validation the stepper reports busy and errors from. */
    nameValidation: OrganizationNameStepValidation<SetupOrganization>;
    /** Whether the organization name is being validated. */
    isNameValidating: boolean;
    /** The legal terms step is shown only for configured documents, with this version. */
    legal: { isConfigured: boolean; version: string };
    /** Opens a legal document. */
    onShowDocument: (kind: LegalDocumentKind) => void;
    /** Records the organization name at submit time. */
    onOrganizationName: (organizationName: string) => void;
    /** Called once the command succeeded. */
    onSuccess: () => Promise<void>;
};

/**
 * The organization setup wizard: the organization step, the user information step and, when legal
 * documents are configured, the terms step.
 * @param props The command wiring, the name validation and the legal document settings.
 * @returns The wizard.
 */
export const OrganizationSetupWizard = ({ invitationKey, initialValues, nameValidation, isNameValidating, legal, onShowDocument, onOrganizationName, onSuccess }: OrganizationSetupWizardProps) => (
    <CommandStepper<SetupOrganization>
        key={invitationKey}
        command={SetupOrganization}
        validateOnInit
        errorDisplayComponent={FieldError}
        onFieldValidate={validateChangedName}
        isBusy={isNameValidating}
        onFieldChange={nameValidation.onFieldChange}
        okLabel={strings.organizationSetup.setupOrganization}
        initialValues={initialValues}
        onBeforeExecute={(values) => {
            onOrganizationName(values.organizationName ?? '');
            return values;
        }}
        onSuccess={onSuccess}
    >
        <StepperPanel header={strings.organizationSetup.stepOrganization}>
            <InitialNameErrors includeOrganization />
            <InputTextField<SetupOrganization>
                value={c => c.organizationName}
                title={strings.organizationSetup.organizationName}
                placeholder={strings.organizationSetup.organizationNamePlaceholder}
                pt={{ root: { autoComplete: 'organization' } }}
            />
            <OrganizationNameStepError validation={nameValidation} />
        </StepperPanel>
        <StepperPanel header={strings.organizationSetup.stepUserInformation}>
            <InputTextField<SetupOrganization>
                value={c => c.firstName}
                title={strings.organizationSetup.firstName}
                placeholder={strings.organizationSetup.firstNamePlaceholder}
                pt={{ root: { autoComplete: 'given-name' } }}
            />
            <InputTextField<SetupOrganization>
                value={c => c.middleName}
                title={strings.organizationSetup.middleName}
                placeholder={strings.organizationSetup.middleNamePlaceholder}
                pt={{ root: { autoComplete: 'additional-name' } }}
            />
            <InputTextField<SetupOrganization>
                value={c => c.lastName}
                title={strings.organizationSetup.lastName}
                placeholder={strings.organizationSetup.lastNamePlaceholder}
                pt={{ root: { autoComplete: 'family-name' } }}
            />
        </StepperPanel>
        {legal.isConfigured && (
            <StepperPanel header={strings.organizationSetup.stepTermsConditions}>
                <LegalVersionValues version={legal.version} />
                <LegalAcceptanceField<SetupOrganization> value={c => c.acceptedLegalTerms} onShowDocument={onShowDocument} />
            </StepperPanel>
        )}
    </CommandStepper>
);
