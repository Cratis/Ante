// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Button } from '@cratis/components/Common';
import { ProgressSpinner } from '@cratis/components/Display';
import { InputTextField } from '@cratis/components/CommandForm';
import { CommandStepper, StepperPanel } from '@cratis/components/CommandDialog';
import { useIdentity } from '@cratis/arc.react/identity';
import { Guid } from '@cratis/fundamentals';
import { AcceptInvitation, StatusForInvitation } from './UserSetup';
import { UserSetupAcceptanceStatus } from './UserSetupAcceptanceStatus';
import { useOnboardingRecovery } from '../useOnboardingRecovery';
import { hasReadStatus } from '../hasReadStatus';
import { useHostOutcome } from '../useHostOutcome';
import { resolveHostOutcomeGate } from '../HostOutcomeGate';
import { HostOutcomeStatus } from '../HostOutcome/HostOutcomeStatus';
import { HostUrl } from '../../Configuration/Configuration';
import { useFreshLegalDocuments } from '../../Legal/useFreshLegalDocuments';
import { UserSetupFrame } from './UserSetupFrame';
import { InvitationIdentityDetails } from '../Accepting/Accepting';
import { getInvitationIdFromToken } from '../Accepting/invitationToken';
import { LegalAcceptanceField } from '../../Legal/LegalAcceptanceField';
import { LegalDocumentsUnavailable } from '../../Legal/LegalDocumentsUnavailable';
import { LegalVersionValues } from '../../Legal/LegalVersionValues';
import { useLegalDocumentViewer } from '../../Legal/useLegalDocumentViewer';
import { ErrorSummary } from '../../Accessibility/ErrorSummary';
import { LiveRegion } from '../../Accessibility/LiveRegion';
import { useAccessibleStepper } from '../../Accessibility/useAccessibleStepper';
import { InitialNameErrors } from '../InitialNameErrors';
import { initialUserSetupValues } from '../initialOnboardingValues';
import { validateChangedName } from '../NameFieldValidation';
import { serverValidationMessages } from './serverValidationMessages';
import strings from 'Strings';

type UserSetupPageProps = {
    invitationToken?: string | null;
};

export const UserSetupPage = ({ invitationToken }: UserSetupPageProps) => {
    const identity = useIdentity(InvitationIdentityDetails);
    const invitationIdentityDetails = identity.details as unknown as InvitationIdentityDetails | undefined;
    const invitationId = useMemo(() => {
        const invitationIdFromToken = getInvitationIdFromToken(invitationToken);
        if (invitationIdFromToken) return invitationIdFromToken;

        if (!identity.isSet || !identity.details) return null;
        const id = invitationIdentityDetails?.invitationId;
        if (!id) return null;
        const str = id.toString();
        return Guid.isGuid(str) ? Guid.parse(str) : null;
    }, [identity.isSet, invitationIdentityDetails, invitationToken]);
    const [errorMessages, setErrorMessages] = useState<string[]>([]);
    const [validationMessages, setValidationMessages] = useState<string[]>([]);
    const resolvedInvitationId = invitationId ?? Guid.empty;
    const [statusResult] = StatusForInvitation.use({ invitationId: resolvedInvitationId });
    const [hostUrlResult] = HostUrl.use();
    const { documents: availableDocuments, lastAvailableDocuments, isChecking: checkingLegalDocuments, refresh: refreshLegalDocuments } = useFreshLegalDocuments();
    const displayedDocuments = availableDocuments ?? lastAvailableDocuments;

    const hasStatus = hasReadStatus(statusResult);
    const isRecorded = hasStatus && statusResult.data.status !== UserSetupAcceptanceStatus.pending;
    const isAccepted = hasStatus && statusResult.data.status === UserSetupAcceptanceStatus.accepted;
    const recovery = useOnboardingRecovery(isRecorded, isAccepted);

    // Never looked up before Ante's own onboarding has actually published - a host has nothing to report
    // on an invitation it has not been notified of accepting yet.
    const hostOutcome = useHostOutcome(recovery.isAccepted ? resolvedInvitationId : Guid.empty);
    const hostOutcomeGate = resolveHostOutcomeGate(recovery.isAccepted, hostOutcome.isConfigured);

    // Static form defaults apply once per command; legal document updates must not replay names.
    const initialValues = useMemo(() => initialUserSetupValues(resolvedInvitationId), [resolvedInvitationId]);

    const legalDocuments = useLegalDocumentViewer({
        termsAndConditions: displayedDocuments?.termsAndConditions ?? '',
        privacyPolicy: displayedDocuments?.privacyPolicy ?? ''
    });

    const stepperContainerRef = useRef<HTMLDivElement>(null);
    const { announcement } = useAccessibleStepper(stepperContainerRef, {
        idPrefix: 'user-setup',
        announcementTemplate: strings.accessibility.stepAnnouncement
    });

    const navigateToHost = useCallback(() => {
        if (!hostUrlResult.isSuccess) {
            setErrorMessages([strings.userSetup.hostAppUrlUnavailable]);
            return;
        }

        try {
            // The provider-specific sign-in path challenges the provider the person just used, so
            // entering the host application is a silent round trip instead of a second provider
            // selection.
            const config = hostUrlResult.data;
            window.location.href = config.signInPath && config.signInPath !== '/'
                ? new URL(config.signInPath, new URL(config.hostAppUrl, window.location.origin)).toString()
                : config.hostAppUrl;
        } catch {
            // A malformed host configuration must present as a recoverable destination failure, not a
            // crashed page - acceptance already published, so the person's information was not lost.
            setErrorMessages([strings.userSetup.hostAppUrlUnavailable]);
        }
    }, [hostUrlResult]);

    useEffect(() => {
        // Unchanged automatic redirect when the optional host-outcome screen was never configured -
        // 'showHostOutcome' instead lets the completion screen's own Continue action call
        // navigateToHost, and 'keepWaiting' means acceptance has not published yet.
        if (hostOutcomeGate !== 'redirectAutomatically') return;
        navigateToHost();
    }, [hostOutcomeGate, navigateToHost]);

    if (errorMessages.length > 0) {
        return (
            <UserSetupFrame>
                <div className='user-setup-card__content'>
                    <ErrorSummary messages={errorMessages} className='user-setup-errors' itemClassName='user-setup-errors__item' />
                </div>
            </UserSetupFrame>
        );
    }

    // Identity/invitation resolution and the first durable status read both have to complete before it
    // is safe to decide between the form and the waiting phase - showing the form even briefly beforehand
    // would let a recovered, already-submitted acceptance flash a form it must never resubmit.
    if ((!invitationId && !identity.isSet) || !hasStatus) {
        return (
            <UserSetupFrame>
                <div className='user-setup-card__content'>
                    <div className='user-setup-waiting'>
                        <ProgressSpinner className='user-setup-waiting__spinner' />
                    </div>
                </div>
            </UserSetupFrame>
        );
    }

    if (recovery.phase === 'timedOut') {
        return (
            <UserSetupFrame>
                <div className='user-setup-card__content'>
                    <div className='user-setup-waiting' role='status' aria-live='polite'>
                        <p className='user-setup-waiting__message'>{strings.onboarding.notYetConfirmed}</p>
                        <Button label={strings.onboarding.checkAgain} onClick={recovery.checkAgain} />
                        <p className='user-setup-waiting__message'>{strings.onboarding.contactSupport}</p>
                    </div>
                </div>
            </UserSetupFrame>
        );
    }

    // Checked before the generic 'waiting' phase below: OnboardingRecoveryState reports 'waiting' for
    // both "recorded but not yet accepted" and "accepted, about to redirect" - once a host outcome
    // adapter is configured, the latter must render this completion screen instead of an indefinite
    // spinner, since navigateToHost is no longer called automatically. Acceptance already published by
    // this point regardless of what (if anything) is shown here; a failed or still-pending host outcome
    // never blocks the Continue action.
    if (hostOutcomeGate === 'showHostOutcome') {
        const message = hostOutcome.status === HostOutcomeStatus.succeeded
            ? strings.userSetup.hostOutcomeSucceeded
            : hostOutcome.status === HostOutcomeStatus.failed
                ? strings.userSetup.hostOutcomeFailed
                : strings.userSetup.hostOutcomePending;

        return (
            <UserSetupFrame>
                <div className='user-setup-card__content'>
                    <div className='user-setup-waiting' role='status' aria-live='polite'>
                        <p className='user-setup-waiting__message'>{message}</p>
                        {hostOutcome.status === HostOutcomeStatus.failed && hostOutcome.reasonCode && (
                            <p className='user-setup-waiting__message'>
                                {strings.onboarding.hostOutcomeReference.replace('{reasonCode}', hostOutcome.reasonCode)}
                            </p>
                        )}
                        <div className='user-setup-host-outcome__actions'>
                            <Button label={strings.onboarding.continueToHost} onClick={navigateToHost} />
                            {hostOutcome.status !== HostOutcomeStatus.succeeded && (
                                <Button label={strings.onboarding.checkAgain} variant='ghost' onClick={hostOutcome.checkAgain} />
                            )}
                        </div>
                    </div>
                </div>
            </UserSetupFrame>
        );
    }

    if (recovery.phase === 'waiting') {
        return (
            <UserSetupFrame>
                <div className='user-setup-card__content'>
                    <div className='user-setup-waiting'>
                        <ProgressSpinner className='user-setup-waiting__spinner' aria-label={strings.userSetup.setting} />
                        <p className='user-setup-waiting__message'>{strings.userSetup.setting}</p>
                    </div>
                </div>
            </UserSetupFrame>
        );
    }

    if (checkingLegalDocuments && !displayedDocuments) {
        return (
            <UserSetupFrame>
                <div className='user-setup-card__content'>
                    <ProgressSpinner aria-label={strings.onboarding.legalDocumentsChecking} />
                </div>
            </UserSetupFrame>
        );
    }

    if (!displayedDocuments) {
        return (
            <UserSetupFrame>
                <div className='user-setup-card__content'>
                    <LegalDocumentsUnavailable onRetry={() => { void refreshLegalDocuments(); }} />
                </div>
            </UserSetupFrame>
        );
    }

    const userInformationPanel = (
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
    );

    return (
        <UserSetupFrame>
            <div className='user-setup-card__content user-setup-card__content--stepper' ref={stepperContainerRef}>
                {validationMessages.length > 0 && (
                    <ErrorSummary messages={validationMessages} className='user-setup-errors' itemClassName='user-setup-errors__item' />
                )}
                {!availableDocuments && (checkingLegalDocuments
                    ? <ProgressSpinner aria-label={strings.onboarding.legalDocumentsChecking} />
                    : <LegalDocumentsUnavailable onRetry={() => { void refreshLegalDocuments(); }} />)}
                <div hidden={!availableDocuments} inert={!availableDocuments}>
                <CommandStepper<AcceptInvitation>
                    key={resolvedInvitationId.toString()}
                    command={AcceptInvitation}
                    validateOnInit
                    onFieldValidate={validateChangedName}
                    onFieldChange={() => setValidationMessages([])}
                    okLabel={strings.userSetup.acceptInvitation}
                    initialValues={initialValues}
                    onSuccess={async () => { recovery.markSubmitted(); }}
                    onValidationFailure={(validationResults) => {
                        // A rejection for a name on an earlier step must remain visible from the terms step.
                        setValidationMessages(serverValidationMessages(validationResults));
                    }}
                >
                    {/* Every child here has to be a StepperPanel. A component that renders one is not one:
                        the stepper counts it as a step but never displays it, which would push the terms step
                        into the position the wizard treats as the last one — so Next would submit the
                        invitation with the terms still unaccepted instead of showing them. */}
                    {userInformationPanel}
                    {displayedDocuments.isConfigured && (
                        <StepperPanel header={strings.userSetup.stepTermsConditions}>
                            <LegalVersionValues version={displayedDocuments.version} />
                            <LegalAcceptanceField<AcceptInvitation> value={c => c.acceptedLegalTerms} onShowDocument={legalDocuments.showDocument} />
                        </StepperPanel>
                    )}
                </CommandStepper>
                </div>
                {availableDocuments && legalDocuments.dialog}
            </div>
            <LiveRegion message={announcement} />
        </UserSetupFrame>
    );
};
