// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ReactElement, ReactNode } from 'react';
import { Button } from '@cratis/components/Common';
import { ProgressSpinner } from '@cratis/components/Display';
import { UserSetupFrame } from './UserSetupFrame';
import { invitationCompletionMessage } from '../invitationCompletionMessage';
import { HostOutcomeStatus } from '../HostOutcome/HostOutcomeStatus';
import { LegalDocumentsUnavailable } from '../../Legal/LegalDocumentsUnavailable';
import { ErrorSummary } from '../../Accessibility/ErrorSummary';
import type { InvitationPageContent } from '../../Configuration/Configuration';
import strings from 'Strings';

type ScreenProps = { content: InvitationPageContent | undefined; children: ReactNode };

type HostOutcomeScreenProps = {
    content: InvitationPageContent | undefined;
    hostOutcome: { status: HostOutcomeStatus; reasonCode: string; requiresNewInvitation: boolean; checkAgain: () => void };
    onContinue: () => void;
};

const hostOutcomeMessage = ({ content, hostOutcome }: Pick<HostOutcomeScreenProps, 'content' | 'hostOutcome'>) => {
    if (hostOutcome.status === HostOutcomeStatus.succeeded) return invitationCompletionMessage(content, strings.userSetup.hostOutcomeSucceeded);
    if (hostOutcome.requiresNewInvitation) return strings.userSetup.hostOutcomeRequiresNewInvitation;
    if (hostOutcome.status === HostOutcomeStatus.failed) return strings.userSetup.hostOutcomeFailed;
    return strings.userSetup.hostOutcomePending;
};

/** The framed card every non-form user setup screen renders inside. */
export const UserSetupScreen = ({ content, children }: ScreenProps) => (
    <UserSetupFrame content={content}>
        <div className='user-setup-card__content'>
            {children}
        </div>
    </UserSetupFrame>
);

/** Acceptance published but the host destination could not be resolved or reached. */
export const UserSetupErrorScreen = ({ content, messages }: { content: InvitationPageContent | undefined; messages: string[] }) => (
    <UserSetupScreen content={content}>
        <ErrorSummary messages={messages} className='user-setup-errors' itemClassName='user-setup-errors__item' />
    </UserSetupScreen>
);

/** The bare spinner shown until identity and the first durable status read resolve. */
export const UserSetupResolvingScreen = ({ content }: { content: InvitationPageContent | undefined }) => (
    <UserSetupScreen content={content}>
        <div className='user-setup-waiting'>
            <ProgressSpinner className='user-setup-waiting__spinner' />
        </div>
    </UserSetupScreen>
);

/** Recovery gave up waiting for confirmation. */
export const UserSetupTimedOutScreen = ({ content, onCheckAgain }: { content: InvitationPageContent | undefined; onCheckAgain: () => void }) => (
    <UserSetupScreen content={content}>
        <div className='user-setup-waiting' role='status' aria-live='polite'>
            <p className='user-setup-waiting__message'>{strings.onboarding.notYetConfirmed}</p>
            <Button label={strings.onboarding.checkAgain} onClick={onCheckAgain} />
            <p className='user-setup-waiting__message'>{strings.onboarding.contactSupport}</p>
        </div>
    </UserSetupScreen>
);

/** The optional host-outcome completion screen. */
export const UserSetupHostOutcomeScreen = ({ content, hostOutcome, onContinue }: HostOutcomeScreenProps) => (
    <UserSetupScreen content={content}>
        <div className='user-setup-waiting' role='status' aria-live='polite'>
            <p className='user-setup-waiting__message'>{hostOutcomeMessage({ content, hostOutcome })}</p>
            {hostOutcome.status === HostOutcomeStatus.failed && hostOutcome.reasonCode && (
                <p className='user-setup-waiting__message'>
                    {strings.onboarding.hostOutcomeReference.replace('{reasonCode}', hostOutcome.reasonCode)}
                </p>
            )}
            <div className='user-setup-host-outcome__actions'>
                {hostOutcome.status === HostOutcomeStatus.succeeded && (
                    <Button label={strings.onboarding.continueToHost} onClick={onContinue} />
                )}
                {hostOutcome.status !== HostOutcomeStatus.succeeded && (
                    <Button label={strings.onboarding.checkAgain} variant='ghost' onClick={hostOutcome.checkAgain} />
                )}
            </div>
        </div>
    </UserSetupScreen>
);

/** Acceptance recorded, waiting for the redirect. */
export const UserSetupSettingUpScreen = ({ content }: { content: InvitationPageContent | undefined }) => (
    <UserSetupScreen content={content}>
        <div className='user-setup-waiting'>
            <ProgressSpinner className='user-setup-waiting__spinner' aria-label={strings.userSetup.setting} />
            <p className='user-setup-waiting__message'>{strings.userSetup.setting}</p>
        </div>
    </UserSetupScreen>
);

/** The first legal document check is still running. */
export const UserSetupLegalCheckingScreen = ({ content }: { content: InvitationPageContent | undefined }) => (
    <UserSetupScreen content={content}>
        <ProgressSpinner aria-label={strings.onboarding.legalDocumentsChecking} />
    </UserSetupScreen>
);

type StatusScreenOptions = {
    content: InvitationPageContent | undefined;
    errorMessages: string[];
    /** Identity/invitation resolution or the first durable status read is still pending. */
    isResolving: boolean;
    recoveryPhase: string;
    onCheckAgain: () => void;
    isShowingHostOutcome: boolean;
    hostOutcome: HostOutcomeScreenProps['hostOutcome'];
    onContinue: () => void;
};

/**
 * The screen for the acceptance status, or undefined when the form (or a legal document screen) is next.
 * @param options The status, recovery and host outcome state.
 * @returns The screen to render, if any.
 */
export const resolveUserSetupStatusScreen = ({ content, errorMessages, isResolving, recoveryPhase, onCheckAgain, isShowingHostOutcome, hostOutcome, onContinue }: StatusScreenOptions): ReactElement | undefined => {
    if (errorMessages.length > 0) return <UserSetupErrorScreen content={content} messages={errorMessages} />;

    // Identity/invitation resolution and the first durable status read both have to complete before it
    // is safe to decide between the form and the waiting phase - showing the form even briefly beforehand
    // would let a recovered, already-submitted acceptance flash a form it must never resubmit.
    if (isResolving) return <UserSetupResolvingScreen content={content} />;

    if (recoveryPhase === 'timedOut') return <UserSetupTimedOutScreen content={content} onCheckAgain={onCheckAgain} />;

    // Checked before the generic 'waiting' phase below: OnboardingRecoveryState reports 'waiting' for
    // both "recorded but not yet accepted" and "accepted, about to redirect" - once a host outcome
    // adapter is configured, the latter must render this completion screen instead of an indefinite
    // spinner, since navigateToHost is no longer called automatically. Acceptance already published by
    // this point regardless of what (if anything) is shown here; a failed or still-pending host outcome
    // never blocks the Continue action.
    if (isShowingHostOutcome) return <UserSetupHostOutcomeScreen content={content} hostOutcome={hostOutcome} onContinue={onContinue} />;

    if (recoveryPhase === 'waiting') return <UserSetupSettingUpScreen content={content} />;

    return undefined;
};

/** No legal documents were ever available. */
export const UserSetupLegalUnavailableScreen = ({ content, onRetry }: { content: InvitationPageContent | undefined; onRetry: () => void }) => (
    <UserSetupScreen content={content}>
        <LegalDocumentsUnavailable onRetry={onRetry} />
    </UserSetupScreen>
);
