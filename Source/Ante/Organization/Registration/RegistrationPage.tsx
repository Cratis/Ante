// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, useRef } from 'react';
import { Button } from '@cratis/components/Common';
import { ProgressSpinner } from '@cratis/components/Display';
import { InputTextField } from '@cratis/components/CommandForm';
import { CommandStepper, StepperPanel } from '@cratis/components/CommandDialog';
import { RegisterOrganization } from './Registration';
import { getOrCreateRegistrationId, clearRegistrationOperation } from './RegistrationOperation';
import { useOrganizationSetupHandoff } from '../../Invitations/OrganizationSetup/useOrganizationSetupHandoff';
import { OrganizationSetupFrame } from '../../Invitations/OrganizationSetup/OrganizationSetupFrame';
import { Current as LegalDocumentsCurrent } from '../../Legal/LegalDocuments';
import { LegalAcceptanceField } from '../../Legal/LegalAcceptanceField';
import { useLegalDocumentViewer } from '../../Legal/useLegalDocumentViewer';
import { ErrorSummary } from '../../Accessibility/ErrorSummary';
import { LiveRegion } from '../../Accessibility/LiveRegion';
import { useAccessibleStepper } from '../../Accessibility/useAccessibleStepper';
import strings from 'Strings';

export const RegistrationPage = () => {
    // Persisted per-tab (not merely a mount-local value) so a reload or a return later resumes polling
    // the same durable registration instead of losing track of what was already submitted - see
    // RegistrationOperation.ts for what is, and is never, stored.
    const registrationId = useMemo(() => getOrCreateRegistrationId(), []);
    const [legalStatus] = LegalDocumentsCurrent.use();

    const handoff = useOrganizationSetupHandoff({
        invitationId: registrationId,
        hostAppUnavailableMessage: strings.registration.hostAppUrlUnavailable,
    });

    // Memoized so an unrelated re-render - opening the terms dialog, a status poll tick - does not
    // recreate this object: CommandForm reasserts initialValues/currentValues onto the command whenever
    // their identity changes, and a fresh literal on every render would otherwise silently uncheck the
    // acceptance box or blank the version as often as the page re-renders.
    const initialValues = useMemo(() => ({ registrationId }), [registrationId]);

    // The legal version comes from a query, so it arrives after mount - it has to be a reactive overlay
    // rather than part of the synchronous baseline, or the command would submit an empty version and be
    // rejected. Memoized on the configured/version pair rather than recreated every render, so it is
    // only reapplied when the document the host presents actually changes - at which point
    // acceptedLegalTerms is deliberately reset to false too, renewing review: a version bump the user
    // has not seen forces a fresh acceptance instead of silently carrying the old one forward. Only
    // these two legal fields are ever included here, so a version change never touches the name fields
    // the user has already filled in. If the source stops being configured entirely, this becomes
    // undefined and any previously accepted value is left as-is on the command; that submission is
    // rejected server-side as unsolicited acceptance rather than silently recorded or silently dropped.
    const currentValues = useMemo(
        () => (legalStatus.data?.isConfigured
            ? { acceptedLegalTerms: false, acceptedLegalVersion: legalStatus.data.version }
            : undefined),
        [legalStatus.data?.isConfigured, legalStatus.data?.version]);

    const legalDocuments = useLegalDocumentViewer({
        termsAndConditions: legalStatus.data?.termsAndConditions ?? '',
        privacyPolicy: legalStatus.data?.privacyPolicy ?? ''
    });

    const stepperContainerRef = useRef<HTMLDivElement>(null);
    const { announcement } = useAccessibleStepper(stepperContainerRef, {
        idPrefix: 'registration',
        announcementTemplate: strings.accessibility.stepAnnouncement
    });

    const startNewRegistration = () => {
        clearRegistrationOperation();
        window.location.reload();
    };

    if (handoff.errorMessages.length > 0) {
        return (
            <OrganizationSetupFrame subtitle={strings.registration.subtitle}>
                <div className='organization-setup-card__content'>
                    <ErrorSummary
                        messages={handoff.errorMessages}
                        className='organization-setup-errors'
                        itemClassName='organization-setup-errors__item'
                    />
                </div>
            </OrganizationSetupFrame>
        );
    }

    // The first durable status read has to complete before it is safe to decide between the form and the
    // waiting phase - showing the form even briefly beforehand would let a recovered, already-submitted
    // registration flash a form it must never resubmit.
    if (!handoff.hasStatus) {
        return (
            <OrganizationSetupFrame subtitle={strings.registration.subtitle}>
                <div className='organization-setup-card__content'>
                    <div className='organization-setup-waiting'>
                        <ProgressSpinner className='organization-setup-waiting__spinner' />
                    </div>
                </div>
            </OrganizationSetupFrame>
        );
    }

    if (handoff.phase === 'timedOut') {
        return (
            <OrganizationSetupFrame subtitle={strings.registration.subtitle}>
                <div className='organization-setup-card__content'>
                    <div className='organization-setup-waiting' role='status' aria-live='polite'>
                        <p className='organization-setup-waiting__message'>{strings.onboarding.notYetConfirmed}</p>
                        <Button label={strings.onboarding.checkAgain} onClick={handoff.checkAgain} />
                        <p className='organization-setup-waiting__message'>{strings.onboarding.contactSupport}</p>
                        <Button label={strings.registration.startNewRegistration} variant='ghost' onClick={startNewRegistration} />
                    </div>
                </div>
            </OrganizationSetupFrame>
        );
    }

    if (handoff.phase === 'waiting') {
        return (
            <OrganizationSetupFrame subtitle={strings.registration.subtitle}>
                <div className='organization-setup-card__content'>
                    <div className='organization-setup-waiting'>
                        <ProgressSpinner className='organization-setup-waiting__spinner' aria-label={strings.organizationSetup.settingUp} />
                        <p className='organization-setup-waiting__message'>{strings.organizationSetup.settingUp}</p>
                    </div>
                </div>
            </OrganizationSetupFrame>
        );
    }

    return (
        <OrganizationSetupFrame subtitle={strings.registration.subtitle}>
            <div className='organization-setup-card__content organization-setup-card__content--stepper' ref={stepperContainerRef}>
                <CommandStepper<RegisterOrganization>
                    command={RegisterOrganization}
                    validateOnInit
                    okLabel={strings.registration.register}
                    initialValues={initialValues}
                    currentValues={currentValues}
                    onBeforeExecute={(values) => {
                        handoff.captureOrganizationName(values.organizationName ?? '');
                        return values;
                    }}
                    onSuccess={async () => { handoff.markSubmitted(); }}
                >
                    <StepperPanel header={strings.organizationSetup.stepOrganization}>
                        <InputTextField<RegisterOrganization>
                            value={c => c.organizationName}
                            title={strings.registration.organizationName}
                            placeholder={strings.registration.organizationNamePlaceholder}
                            pt={{ root: { autoComplete: 'organization' } }}
                        />
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
                    {legalStatus.data?.isConfigured && (
                        <StepperPanel header={strings.organizationSetup.stepTermsConditions}>
                            <LegalAcceptanceField<RegisterOrganization> value={c => c.acceptedLegalTerms} onShowDocument={legalDocuments.showDocument} />
                        </StepperPanel>
                    )}
                </CommandStepper>
                {legalDocuments.dialog}
            </div>
            <LiveRegion message={announcement} />
        </OrganizationSetupFrame>
    );
};
