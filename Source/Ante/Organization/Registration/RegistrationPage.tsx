// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useMemo, useRef, useState, useSyncExternalStore } from 'react';
import { Button } from '@cratis/components/Common';
import { ProgressSpinner } from '@cratis/components/Display';
import { InputTextField } from '@cratis/components/CommandForm';
import { CommandStepper, StepperPanel } from '@cratis/components/CommandDialog';
import { RegisterOrganization } from './Registration';
import { startRegistration } from './startRegistration';
import { RegistrationStartFailed } from './RegistrationStartFailed';
import { getOrCreateRegistrationOperation, clearRegistrationOperation } from './RegistrationOperation';
import { registrationValidationFailure } from './registrationValidationFailure';
import { shouldResumeRegistrationAfterFailure } from './shouldResumeRegistrationAfterFailure';
import { OrganizationNameStepValidation } from '../OrganizationNameStepValidation';
import { OrganizationNameStepError } from '../OrganizationNameStepError';
import { InitialNameErrors } from '../../Invitations/InitialNameErrors';
import { initialRegistrationValues } from '../../Invitations/initialOnboardingValues';
import { validateChangedName } from '../../Invitations/NameFieldValidation';
import { localeHttpHeaders } from '../../Locale/localeHttpHeaders';
import { useLocale } from '../../Locale/LocaleContext';
import { useOrganizationSetupHandoff } from '../../Invitations/OrganizationSetup/useOrganizationSetupHandoff';
import { OrganizationSetupFrame } from '../../Invitations/OrganizationSetup/OrganizationSetupFrame';
import { useFreshLegalDocuments } from '../../Legal/useFreshLegalDocuments';
import { LegalAcceptanceField } from '../../Legal/LegalAcceptanceField';
import { LegalDocumentsUnavailable } from '../../Legal/LegalDocumentsUnavailable';
import { LegalVersionValues } from '../../Legal/LegalVersionValues';
import { useLegalDocumentViewer } from '../../Legal/useLegalDocumentViewer';
import { ErrorSummary } from '../../Accessibility/ErrorSummary';
import { LiveRegion } from '../../Accessibility/LiveRegion';
import { useAccessibleStepper } from '../../Accessibility/useAccessibleStepper';
import { Registration as RegistrationConfigurationQuery, RegistrationConfiguration } from '../../Configuration/Configuration';
import { RegistrationClosed } from './RegistrationClosed';
import { hasRegistrationConfiguration } from './hasRegistrationConfiguration';
import { RegistrationIntro } from './RegistrationIntro';
import { captureSignupContext, clearSignupContext } from './signupContext';
import { HostOutcomeCompletion } from '../../Invitations/OrganizationSetup/HostOutcomeCompletion';
import strings from 'Strings';

export const RegistrationPage = () => {
    const [configuration] = RegistrationConfigurationQuery.use();
    if (!hasRegistrationConfiguration(configuration)) {
        return (
            <OrganizationSetupFrame subtitle={strings.registration.subtitle}>
                <div className='organization-setup-card__content'>
                    <ProgressSpinner aria-label={strings.registration.loading} />
                </div>
            </OrganizationSetupFrame>
        );
    }

    if (!configuration.data.isEnabled) {
        return (
            <OrganizationSetupFrame subtitle={strings.registration.subtitle}>
                <div className='organization-setup-card__content'>
                    <RegistrationClosed closedUrl={configuration.data.closedUrl} />
                </div>
            </OrganizationSetupFrame>
        );
    }

    return <OpenRegistration configuration={configuration.data} />;
};

const OpenRegistration = ({ configuration }: { configuration: RegistrationConfiguration }) => {
    const content = configuration.content;
    const subtitle = content?.title || strings.registration.subtitle;
    const signupContext = useMemo(
        () => captureSignupContext(configuration.contextKeys ?? [], globalThis.location?.search ?? ''),
        [configuration.contextKeys]);
    // Persisted per-tab (not merely a mount-local value) so a reload or a return later resumes polling
    // the same durable registration instead of losing track of what was already submitted - see
    // RegistrationOperation.ts for what is, and is never, stored.
    const operation = useMemo(() => getOrCreateRegistrationOperation(), []);
    const registrationId = operation.id;
    const [startState, setStartState] = useState<'pending' | 'started' | 'failed'>('pending');
    const [startAttempt, setStartAttempt] = useState(0);
    const { documents: availableDocuments, lastAvailableDocuments, isChecking: checkingLegalDocuments, refresh: refreshLegalDocuments } = useFreshLegalDocuments();
    const displayedDocuments = availableDocuments ?? lastAvailableDocuments;
    const locale = useLocale();

    const handoff = useOrganizationSetupHandoff({
        invitationId: registrationId,
        hostAppUnavailableMessage: strings.registration.hostAppUrlUnavailable,
        isRegistration: true,
        recoveringRegistration: operation.isRecovered,
        supportsHostOutcome: true,
    });
    const markSubmittedRef = useRef(handoff.markSubmitted);
    markSubmittedRef.current = handoff.markSubmitted;
    useEffect(() => {
        if (!handoff.hasStatus || handoff.phase !== 'form') return;
        let active = true;
        void startRegistration(registrationId).then(outcome => {
            if (!active) return;
            if (outcome === 'resume') markSubmittedRef.current();
            else setStartState(outcome);
        });
        return () => { active = false; };
    }, [registrationId, handoff.hasStatus, handoff.phase, startAttempt]);
    const nameValidation = useMemo(() => new OrganizationNameStepValidation(
        () => {
            const probe = new RegisterOrganization();
            probe.registrationId = registrationId;
            probe.setHttpHeadersCallback(localeHttpHeaders);
            return probe;
        },
        () => strings.organizationSetup.nameValidationUnavailable,
        results => { if (shouldResumeRegistrationAfterFailure(results)) markSubmittedRef.current(); }
    ), [registrationId]);
    const { isValidating: isNameValidating, error: nameValidationError } = useSyncExternalStore(nameValidation.subscribe, nameValidation.getSnapshot);
    useEffect(() => () => nameValidation.dispose(), [nameValidation]);
    useEffect(() => nameValidation.onLocaleChange(), [nameValidation, locale]);

    // Without currentValues the form seeds this command once. The legal document version is
    // applied separately when it arrives, without replaying editable defaults on every render.
    const legalDocumentsConfigured = displayedDocuments?.isConfigured ?? false;
    const initialValues = useMemo(() => initialRegistrationValues(registrationId, legalDocumentsConfigured), [registrationId, legalDocumentsConfigured]);

    const legalDocuments = useLegalDocumentViewer({
        termsAndConditions: displayedDocuments?.termsAndConditions ?? '',
        privacyPolicy: displayedDocuments?.privacyPolicy ?? ''
    });

    const { announcement, containerRef: stepperContainerRef } = useAccessibleStepper({
        idPrefix: 'registration',
        announcementTemplate: strings.accessibility.stepAnnouncement
    });

    const startNewRegistration = () => {
        clearRegistrationOperation();
        clearSignupContext();
        window.location.reload();
    };

    if (handoff.errorMessages.length > 0) {
        return (
            <OrganizationSetupFrame subtitle={subtitle}>
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
            <OrganizationSetupFrame subtitle={subtitle}>
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
            <OrganizationSetupFrame subtitle={subtitle}>
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

    if (handoff.phase === 'hostOutcome') {
        return (
            <OrganizationSetupFrame subtitle={subtitle}>
                <div className='organization-setup-card__content'>
                    <HostOutcomeCompletion handoff={handoff} succeededMessage={content?.completionMessage} />
                </div>
            </OrganizationSetupFrame>
        );
    }

    if (handoff.phase === 'waiting') {
        return (
            <OrganizationSetupFrame subtitle={subtitle}>
                <div className='organization-setup-card__content'>
                    <div className='organization-setup-waiting'>
                        <ProgressSpinner className='organization-setup-waiting__spinner' aria-label={strings.organizationSetup.settingUp} />
                        <p className='organization-setup-waiting__message'>{content?.completionMessage || strings.organizationSetup.settingUp}</p>
                    </div>
                </div>
            </OrganizationSetupFrame>
        );
    }

    if (startState !== 'started') {
        return (
            <OrganizationSetupFrame subtitle={subtitle}>
                <div className='organization-setup-card__content'>
                    {startState === 'failed' ? (
                        <RegistrationStartFailed
                            onRetry={() => { setStartState('pending'); setStartAttempt(attempt => attempt + 1); }}
                            onStartNew={startNewRegistration}
                        />
                    ) : <ProgressSpinner aria-label={strings.registration.starting} />}
                </div>
            </OrganizationSetupFrame>
        );
    }

    if (checkingLegalDocuments && !displayedDocuments) {
        return (
            <OrganizationSetupFrame subtitle={subtitle}>
                <div className='organization-setup-card__content'>
                    <ProgressSpinner aria-label={strings.onboarding.legalDocumentsChecking} />
                </div>
            </OrganizationSetupFrame>
        );
    }

    if (!displayedDocuments) {
        return (
            <OrganizationSetupFrame subtitle={subtitle}>
                <div className='organization-setup-card__content'>
                    <LegalDocumentsUnavailable onRetry={() => { void refreshLegalDocuments(); }} />
                </div>
            </OrganizationSetupFrame>
        );
    }

    return (
        <OrganizationSetupFrame subtitle={subtitle}>
            <div className='organization-setup-card__content organization-setup-card__content--stepper' ref={stepperContainerRef}>
                <RegistrationIntro content={content} />
                {!availableDocuments && (checkingLegalDocuments
                    ? <ProgressSpinner aria-label={strings.onboarding.legalDocumentsChecking} />
                    : <LegalDocumentsUnavailable onRetry={() => { void refreshLegalDocuments(); }} />)}
                <div hidden={!availableDocuments} inert={!availableDocuments}>
                <CommandStepper<RegisterOrganization>
                    command={RegisterOrganization}
                    validateOnInit
                    onFieldValidate={validateChangedName}
                    isBusy={isNameValidating}
                    onFieldChange={nameValidation.onFieldChange}
                    okLabel={strings.registration.register}
                    initialValues={initialValues}
                    onBeforeExecute={(values) => {
                        handoff.captureOrganizationName(values.organizationName ?? '');
                        values.signupContext = signupContext;
                        return values;
                    }}
                    onSuccess={async () => { handoff.markSubmitted(); }}
                    {...registrationValidationFailure(handoff.markSubmitted)}
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
                    {displayedDocuments.isConfigured && (
                        <StepperPanel header={strings.organizationSetup.stepTermsConditions}>
                            <LegalVersionValues version={displayedDocuments.version} />
                            <LegalAcceptanceField<RegisterOrganization> value={c => c.acceptedLegalTerms} onShowDocument={legalDocuments.showDocument} />
                        </StepperPanel>
                    )}
                </CommandStepper>
                </div>
                {availableDocuments && legalDocuments.dialog}
            </div>
            <LiveRegion message={announcement} />
            <LiveRegion message={nameValidationError ?? ''} />
        </OrganizationSetupFrame>
    );
};
