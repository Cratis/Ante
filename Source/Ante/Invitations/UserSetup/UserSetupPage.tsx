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
import { useHostOutcome } from '../useHostOutcome';
import { resolveHostOutcomeGate } from '../HostOutcomeGate';
import { HostOutcomeStatus } from '../HostOutcome/HostOutcomeStatus';
import { HostUrl } from '../../Configuration/Configuration';
import { Current as LegalDocumentsCurrent } from '../../Legal/LegalDocuments';
import { UserSetupFrame } from './UserSetupFrame';
import { InvitationIdentityDetails } from '../Accepting/Accepting';
import { getInvitationIdFromToken } from '../Accepting/invitationToken';
import { LegalAcceptanceField } from '../../Legal/LegalAcceptanceField';
import { useLegalDocumentViewer } from '../../Legal/useLegalDocumentViewer';
import { ErrorSummary } from '../../Accessibility/ErrorSummary';
import { LiveRegion } from '../../Accessibility/LiveRegion';
import { useAccessibleStepper } from '../../Accessibility/useAccessibleStepper';
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
    const resolvedInvitationId = invitationId ?? Guid.empty;
    const [statusResult] = StatusForInvitation.use({ invitationId: resolvedInvitationId });
    const [hostUrlResult] = HostUrl.use();
    const [legalStatus] = LegalDocumentsCurrent.use();

    const isRecorded = statusResult.hasData && statusResult.data.status !== UserSetupAcceptanceStatus.pending;
    const isAccepted = statusResult.hasData && statusResult.data.status === UserSetupAcceptanceStatus.accepted;
    const recovery = useOnboardingRecovery(isRecorded, isAccepted);

    // Never looked up before Ante's own onboarding has actually published - a host has nothing to report
    // on an invitation it has not been notified of accepting yet.
    const hostOutcome = useHostOutcome(recovery.isAccepted ? resolvedInvitationId : Guid.empty);
    const hostOutcomeGate = resolveHostOutcomeGate(recovery.isAccepted, hostOutcome.isConfigured);

    // Memoized so an unrelated re-render - opening the terms dialog, a status poll tick - does not
    // recreate this object: CommandForm reasserts initialValues/currentValues onto the command whenever
    // their identity changes, and a fresh literal on every render would otherwise silently uncheck the
    // acceptance box or blank the version as often as the page re-renders.
    const initialValues = useMemo(() => ({ invitationId: resolvedInvitationId }), [resolvedInvitationId]);

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
    if ((!invitationId && !identity.isSet) || !statusResult.hasData) {
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

    const userInformationPanel = (
        <StepperPanel header={strings.userSetup.stepUserInformation}>
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
                <CommandStepper<AcceptInvitation>
                    command={AcceptInvitation}
                    validateOnInit
                    okLabel={strings.userSetup.acceptInvitation}
                    initialValues={initialValues}
                    currentValues={currentValues}
                    onSuccess={async () => { recovery.markSubmitted(); }}
                    onValidationFailure={(validationResults) => {
                        // Acceptance can be rejected for reasons no form field can express - most importantly when the
                        // login already belongs to a user in the organization. Surface those messages rather than
                        // leaving the user on a form that silently refuses to submit.
                        const messages = validationResults.map(result => result.message).filter(message => !!message);
                        setErrorMessages(messages.length > 0 ? messages : [strings.userSetup.acceptanceFailed]);
                    }}
                >
                    {/* Every child here has to be a StepperPanel. A component that renders one is not one:
                        the stepper counts it as a step but never displays it, which would push the terms step
                        into the position the wizard treats as the last one — so Next would submit the
                        invitation with the terms still unaccepted instead of showing them. */}
                    {userInformationPanel}
                    {legalStatus.data?.isConfigured && (
                        <StepperPanel header={strings.userSetup.stepTermsConditions}>
                            <LegalAcceptanceField<AcceptInvitation> value={c => c.acceptedLegalTerms} onShowDocument={legalDocuments.showDocument} />
                        </StepperPanel>
                    )}
                </CommandStepper>
                {legalDocuments.dialog}
            </div>
            <LiveRegion message={announcement} />
        </UserSetupFrame>
    );
};
