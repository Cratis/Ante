// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, useState, type ReactNode } from 'react';
import { useIdentity } from '@cratis/arc.react/identity';
import { Guid } from '@cratis/fundamentals';
import { StatusForInvitation } from './UserSetup';
import { UserSetupAcceptanceStatus } from './UserSetupAcceptanceStatus';
import { useOnboardingRecovery } from '../useOnboardingRecovery';
import { hasReadStatus } from '../hasReadStatus';
import { useHostOutcome } from '../useHostOutcome';
import { resolveHostOutcomeGate } from '../HostOutcomeGate';
import { useFreshLegalDocuments } from '../../Legal/useFreshLegalDocuments';
import { UserSetupFrame } from './UserSetupFrame';
import { useInvitationContent } from '../useInvitationContent';
import { InvitationIdentityDetails } from '../Accepting/Accepting';
import { getInvitationIdFromToken } from '../Accepting/invitationToken';
import { useLegalDocumentViewer } from '../../Legal/useLegalDocumentViewer';
import { LegalGatedForm } from '../../Legal/LegalGatedForm';
import { ErrorSummary } from '../../Accessibility/ErrorSummary';
import { LiveRegion } from '../../Accessibility/LiveRegion';
import { useAccessibleStepper } from '../../Accessibility/useAccessibleStepper';
import { initialUserSetupValues } from '../initialOnboardingValues';
import { serverValidationMessages } from './serverValidationMessages';
import { useUserSetupHostRedirect } from './useUserSetupHostRedirect';
import { UserSetupWizard } from './UserSetupWizard';
import { UserSetupLegalCheckingScreen, UserSetupLegalUnavailableScreen, resolveUserSetupStatusScreen } from './UserSetupScreens';
import strings from 'Strings';

type UserSetupPageProps = {
    invitationToken?: string | null;
};

const resolveInvitationId = (invitationToken: string | null | undefined, isIdentitySet: boolean, details: InvitationIdentityDetails | undefined): Guid | null => {
    const invitationIdFromToken = getInvitationIdFromToken(invitationToken);
    if (invitationIdFromToken) return invitationIdFromToken;

    if (!isIdentitySet || !details) return null;
    const id = details.invitationId;
    if (!id) return null;
    const str = id.toString();
    return Guid.isGuid(str) ? Guid.parse(str) : null;
};

export const UserSetupPage = ({ invitationToken }: UserSetupPageProps) => {
    const content = useInvitationContent('userSetup');
    const identity = useIdentity(InvitationIdentityDetails);
    const invitationIdentityDetails = identity.details as unknown as InvitationIdentityDetails | undefined;
    const invitationId = useMemo(
        () => resolveInvitationId(invitationToken, identity.isSet, identity.details ? invitationIdentityDetails : undefined),
        [identity.isSet, invitationIdentityDetails, invitationToken]);
    const [validationMessages, setValidationMessages] = useState<string[]>([]);
    const resolvedInvitationId = invitationId ?? Guid.empty;
    const [statusResult] = StatusForInvitation.use({ invitationId: resolvedInvitationId });
    const { documents: availableDocuments, lastAvailableDocuments, isChecking: checkingLegalDocuments, refresh: refreshLegalDocuments } = useFreshLegalDocuments();
    const displayedDocuments = availableDocuments ?? lastAvailableDocuments;

    const hasStatus = hasReadStatus(statusResult, resolvedInvitationId);
    const isRecorded = hasStatus && statusResult.data.status !== UserSetupAcceptanceStatus.pending;
    const isAccepted = hasStatus && statusResult.data.status === UserSetupAcceptanceStatus.accepted;
    const recovery = useOnboardingRecovery(isRecorded, isAccepted);

    // Never looked up before Ante's own onboarding has actually published - a host has nothing to report
    // on an invitation it has not been notified of accepting yet.
    const hostOutcome = useHostOutcome(recovery.isAccepted ? resolvedInvitationId : Guid.empty);
    const hostOutcomeGate = resolveHostOutcomeGate(recovery.isAccepted, hostOutcome.isConfigured, hostOutcome.isRead, hostOutcome.status, hostOutcome.hasGivenUp);

    // Static form defaults apply once per command; legal document updates must not replay names.
    const legalDocumentsConfigured = displayedDocuments?.isConfigured ?? false;
    const initialValues = useMemo(() => initialUserSetupValues(resolvedInvitationId, legalDocumentsConfigured), [resolvedInvitationId, legalDocumentsConfigured]);

    const legalDocuments = useLegalDocumentViewer({
        termsAndConditions: displayedDocuments?.termsAndConditions ?? '',
        privacyPolicy: displayedDocuments?.privacyPolicy ?? ''
    });

    const { announcement, containerRef: stepperContainerRef } = useAccessibleStepper({
        idPrefix: 'user-setup',
        announcementTemplate: strings.accessibility.stepAnnouncement
    });

    const { errorMessages, navigateToHost } = useUserSetupHostRedirect(hostOutcomeGate);
    const refreshDocuments = () => { void refreshLegalDocuments(); };

    const statusScreen = resolveUserSetupStatusScreen({
        content, errorMessages, isResolving: (!invitationId && !identity.isSet) || !hasStatus,
        recoveryPhase: recovery.phase, onCheckAgain: recovery.checkAgain,
        isShowingHostOutcome: hostOutcomeGate === 'showHostOutcome', hostOutcome, onContinue: navigateToHost
    });
    if (statusScreen) return statusScreen;

    if (checkingLegalDocuments && !displayedDocuments) return <UserSetupLegalCheckingScreen content={content} />;

    if (!displayedDocuments) return <UserSetupLegalUnavailableScreen content={content} onRetry={refreshDocuments} />;

    const wizard: ReactNode = (
        <UserSetupWizard
            invitationKey={resolvedInvitationId.toString()}
            initialValues={initialValues}
            legal={displayedDocuments}
            onShowDocument={legalDocuments.showDocument}
            onSuccess={async () => { recovery.markSubmitted(); }}
            onFieldChange={() => setValidationMessages([])}
            // A rejection for a name on an earlier step must remain visible from the terms step.
            onValidationFailure={validationResults => setValidationMessages(serverValidationMessages(validationResults))}
        />
    );

    return (
        <UserSetupFrame content={content}>
            <div className='user-setup-card__content user-setup-card__content--stepper' ref={stepperContainerRef}>
                {validationMessages.length > 0 && (
                    <ErrorSummary messages={validationMessages} className='user-setup-errors' itemClassName='user-setup-errors__item' />
                )}
                <LegalGatedForm isAvailable={!!availableDocuments} isChecking={checkingLegalDocuments} onRetry={refreshDocuments} dialog={legalDocuments.dialog}>
                    {wizard}
                </LegalGatedForm>
            </div>
            <LiveRegion message={announcement} />
        </UserSetupFrame>
    );
};
