// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useMemo, useRef, useSyncExternalStore } from 'react';
import { RegisterOrganization } from './Registration';
import { useRegistrationStart } from './useRegistrationStart';
import { RegistrationWizard } from './RegistrationWizard';
import { getOrCreateRegistrationOperation, clearRegistrationOperation } from './RegistrationOperation';
import { shouldResumeRegistrationAfterFailure } from './shouldResumeRegistrationAfterFailure';
import { OrganizationNameStepValidation } from '../OrganizationNameStepValidation';
import { initialRegistrationValues } from '../../Invitations/initialOnboardingValues';
import { localeHttpHeaders } from '../../Locale/localeHttpHeaders';
import { useLocale } from '../../Locale/LocaleContext';
import { useOrganizationSetupHandoff } from '../../Invitations/OrganizationSetup/useOrganizationSetupHandoff';
import { OrganizationSetupFrame } from '../../Invitations/OrganizationSetup/OrganizationSetupFrame';
import { OrganizationSetupScreen } from '../../Invitations/OrganizationSetup/OrganizationSetupScreens';
import { useFreshLegalDocuments } from '../../Legal/useFreshLegalDocuments';
import { LegalGatedForm } from '../../Legal/LegalGatedForm';
import { useLegalDocumentViewer } from '../../Legal/useLegalDocumentViewer';
import { LiveRegion } from '../../Accessibility/LiveRegion';
import { useAccessibleStepper } from '../../Accessibility/useAccessibleStepper';
import { Registration as RegistrationConfigurationQuery, RegistrationConfiguration } from '../../Configuration/Configuration';
import { RegistrationClosed } from './RegistrationClosed';
import { hasRegistrationConfiguration } from './hasRegistrationConfiguration';
import { RegistrationIntro } from './RegistrationIntro';
import { captureSignupContext, clearSignupContext } from './signupContext';
import { RegistrationLoadingScreen, resolveRegistrationHandoffScreen, resolveRegistrationPrerequisiteScreen } from './RegistrationScreens';
import strings from 'Strings';

export const RegistrationPage = () => {
    const [configuration] = RegistrationConfigurationQuery.use();
    if (!hasRegistrationConfiguration(configuration)) return <RegistrationLoadingScreen />;

    if (!configuration.data.isEnabled) {
        return (
            <OrganizationSetupScreen subtitle={strings.registration.subtitle}>
                <RegistrationClosed closedUrl={configuration.data.closedUrl} />
            </OrganizationSetupScreen>
        );
    }

    return <OpenRegistration configuration={configuration.data} />;
};

const startNewRegistration = () => {
    clearRegistrationOperation();
    clearSignupContext();
    window.location.reload();
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
    const { startState, retry: retryStart } = useRegistrationStart({
        registrationId, canStart: handoff.hasStatus && handoff.phase === 'form', markSubmittedRef
    });
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
    const refreshDocuments = () => { void refreshLegalDocuments(); };

    const screen = resolveRegistrationHandoffScreen({ handoff, subtitle, completionMessage: content?.completionMessage, onStartNew: startNewRegistration })
        ?? resolveRegistrationPrerequisiteScreen({
            subtitle, startState, onRetryStart: retryStart, onStartNew: startNewRegistration,
            isCheckingLegalDocuments: checkingLegalDocuments, hasDocuments: !!displayedDocuments, onRetryLegalDocuments: refreshDocuments
        });
    if (screen || !displayedDocuments) return screen;

    return (
        <OrganizationSetupFrame subtitle={subtitle}>
            <div className='organization-setup-card__content organization-setup-card__content--stepper' ref={stepperContainerRef}>
                <RegistrationIntro content={content} />
                <LegalGatedForm isAvailable={!!availableDocuments} isChecking={checkingLegalDocuments} onRetry={refreshDocuments} dialog={legalDocuments.dialog}>
                    <RegistrationWizard
                        initialValues={initialValues}
                        nameValidation={nameValidation}
                        isNameValidating={isNameValidating}
                        legal={displayedDocuments}
                        onShowDocument={legalDocuments.showDocument}
                        onOrganizationName={handoff.captureOrganizationName}
                        signupContext={signupContext}
                        onSubmitted={handoff.markSubmitted}
                    />
                </LegalGatedForm>
            </div>
            <LiveRegion message={announcement} />
            <LiveRegion message={nameValidationError ?? ''} />
        </OrganizationSetupFrame>
    );
};
