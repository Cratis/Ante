// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useMemo, useSyncExternalStore } from 'react';
import { Guid } from '@cratis/fundamentals';
import { SetupOrganization } from './OrganizationSetup';
import { useOrganizationSetupHandoff } from './useOrganizationSetupHandoff';
import { useInvitationId } from './useInvitationId';
import { OrganizationNameStepValidation } from '../../Organization/OrganizationNameStepValidation';
import { initialOrganizationSetupValues } from '../initialOnboardingValues';
import { localeHttpHeaders } from '../../Locale/localeHttpHeaders';
import { useLocale } from '../../Locale/LocaleContext';
import { useFreshLegalDocuments } from '../../Legal/useFreshLegalDocuments';
import { OrganizationSetupFrame } from './OrganizationSetupFrame';
import { useInvitationContent } from '../useInvitationContent';
import { invitationCompletionMessage } from '../invitationCompletionMessage';
import { useLegalDocumentViewer } from '../../Legal/useLegalDocumentViewer';
import { LegalGatedForm } from '../../Legal/LegalGatedForm';
import { LiveRegion } from '../../Accessibility/LiveRegion';
import { useAccessibleStepper } from '../../Accessibility/useAccessibleStepper';
import { OrganizationSetupWizard } from './OrganizationSetupWizard';
import {
    OrganizationSetupErrorScreen,
    OrganizationSetupHostOutcomeScreen,
    OrganizationSetupLegalCheckingScreen,
    OrganizationSetupLegalUnavailableScreen,
    OrganizationSetupResolvingScreen,
    OrganizationSetupSettingUpScreen,
    OrganizationSetupTimedOutScreen
} from './OrganizationSetupScreens';
import strings from 'Strings';

type OrganizationSetupPageProps = {
    invitationToken?: string | null;
};

export const OrganizationSetupPage = ({ invitationToken }: OrganizationSetupPageProps) => {
    const content = useInvitationContent('organizationSetup');
    const { invitationId, isIdentitySet } = useInvitationId(invitationToken);
    const resolvedInvitationId = invitationId ?? Guid.empty;
    const invitationIdText = resolvedInvitationId.toString();
    const { documents: availableDocuments, lastAvailableDocuments, isChecking: checkingLegalDocuments, refresh: refreshLegalDocuments } = useFreshLegalDocuments();
    const displayedDocuments = availableDocuments ?? lastAvailableDocuments;
    const locale = useLocale();

    const handoff = useOrganizationSetupHandoff({
        invitationId: resolvedInvitationId,
        hostAppUnavailableMessage: strings.organizationSetup.hostAppUrlUnavailable,
        supportsHostOutcome: true,
    });
    const nameValidation = useMemo(() => new OrganizationNameStepValidation(
        () => {
            const probe = new SetupOrganization();
            probe.invitationId = Guid.parse(invitationIdText);
            probe.setHttpHeadersCallback(localeHttpHeaders);
            return probe;
        },
        () => strings.organizationSetup.nameValidationUnavailable
    ), [invitationIdText]);
    const { isValidating: isNameValidating, error: nameValidationError } = useSyncExternalStore(nameValidation.subscribe, nameValidation.getSnapshot);
    useEffect(() => () => nameValidation.dispose(), [nameValidation]);
    useEffect(() => nameValidation.onLocaleChange(), [nameValidation, locale]);

    // Seed editable fields once; later legal document updates must not replay these defaults.
    const legalDocumentsConfigured = displayedDocuments?.isConfigured ?? false;
    const initialValues = useMemo(() => initialOrganizationSetupValues(resolvedInvitationId, legalDocumentsConfigured), [resolvedInvitationId, legalDocumentsConfigured]);

    const legalDocuments = useLegalDocumentViewer({
        termsAndConditions: displayedDocuments?.termsAndConditions ?? '',
        privacyPolicy: displayedDocuments?.privacyPolicy ?? ''
    });

    const { announcement, containerRef: stepperContainerRef } = useAccessibleStepper({
        idPrefix: 'organization-setup',
        announcementTemplate: strings.accessibility.stepAnnouncement
    });
    const refreshDocuments = () => { void refreshLegalDocuments(); };

    if (handoff.errorMessages.length > 0) return <OrganizationSetupErrorScreen content={content} messages={handoff.errorMessages} />;

    // Identity/invitation resolution and the first durable status read both have to complete before it
    // is safe to decide between the form and the waiting phase - showing the form even briefly beforehand
    // would let a recovered, already-submitted operation flash a form it must never resubmit.
    const isResolving = (!invitationId && !isIdentitySet) || !handoff.hasStatus;
    if (isResolving) return <OrganizationSetupResolvingScreen content={content} />;

    if (handoff.phase === 'timedOut') return <OrganizationSetupTimedOutScreen content={content} onCheckAgain={handoff.checkAgain} />;

    if (handoff.phase === 'waiting') {
        return <OrganizationSetupSettingUpScreen content={content} message={strings.organizationSetup.settingUp} />;
    }

    // Only reached when this deployment has a host outcome backchannel configured - see
    // useOrganizationSetupHandoff's supportsHostOutcome option. Organization setup already published by
    // this point regardless of what (if anything) is shown here; a failed or still-pending host outcome
    // never blocks the Continue action below.
    if (handoff.phase === 'hostOutcome') {
        return <OrganizationSetupHostOutcomeScreen content={content} handoff={handoff} succeededMessage={invitationCompletionMessage(content, strings.organizationSetup.hostOutcomeSucceeded)} />;
    }

    if (checkingLegalDocuments && !displayedDocuments) return <OrganizationSetupLegalCheckingScreen content={content} />;

    if (!displayedDocuments) return <OrganizationSetupLegalUnavailableScreen content={content} onRetry={refreshDocuments} />;

    return (
        <OrganizationSetupFrame content={content}>
            <div className='organization-setup-card__content organization-setup-card__content--stepper' ref={stepperContainerRef}>
                <LegalGatedForm isAvailable={!!availableDocuments} isChecking={checkingLegalDocuments} onRetry={refreshDocuments} dialog={legalDocuments.dialog}>
                    <OrganizationSetupWizard
                        invitationKey={invitationIdText}
                        initialValues={initialValues}
                        nameValidation={nameValidation}
                        isNameValidating={isNameValidating}
                        legal={displayedDocuments}
                        onShowDocument={legalDocuments.showDocument}
                        onOrganizationName={handoff.captureOrganizationName}
                        onSuccess={async () => { handoff.markSubmitted(); }}
                    />
                </LegalGatedForm>
            </div>
            <LiveRegion message={announcement} />
            <LiveRegion message={nameValidationError ?? ''} />
        </OrganizationSetupFrame>
    );
};
