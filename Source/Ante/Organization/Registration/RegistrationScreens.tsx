// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ReactElement } from 'react';
import { Button } from '@cratis/components/Common';
import { ProgressSpinner } from '@cratis/components/Display';
import { RegistrationStartFailed } from './RegistrationStartFailed';
import {
    OrganizationSetupErrorScreen,
    OrganizationSetupHostOutcomeScreen,
    OrganizationSetupLegalCheckingScreen,
    OrganizationSetupLegalUnavailableScreen,
    OrganizationSetupResolvingScreen,
    OrganizationSetupScreen,
    OrganizationSetupSettingUpScreen,
    OrganizationSetupTimedOutScreen
} from '../../Invitations/OrganizationSetup/OrganizationSetupScreens';
import type { OrganizationSetupHandoffState } from '../../Invitations/OrganizationSetup/useOrganizationSetupHandoff';
import strings from 'Strings';

type RegistrationScreenContext = {
    /** The frame subtitle. */
    subtitle: string;
    /** The host-authored completion message, when configured. */
    completionMessage?: string;
};

type HandoffScreenOptions = RegistrationScreenContext & {
    handoff: OrganizationSetupHandoffState;
    /** Clears the persisted registration and starts over. */
    onStartNew: () => void;
};

type PrerequisiteScreenOptions = RegistrationScreenContext & {
    startState: 'pending' | 'started' | 'failed';
    onRetryStart: () => void;
    onStartNew: () => void;
    isCheckingLegalDocuments: boolean;
    hasDocuments: boolean;
    onRetryLegalDocuments: () => void;
};

/** Shown while the registration configuration has not arrived. */
export const RegistrationLoadingScreen = () => (
    <OrganizationSetupScreen subtitle={strings.registration.subtitle}>
        <ProgressSpinner aria-label={strings.registration.loading} />
    </OrganizationSetupScreen>
);

const reviewMessage = (phase: OrganizationSetupHandoffState['phase']) =>
    phase === 'heldForReview' ? strings.registration.waitingForReview : strings.registration.declined;

/**
 * The screen for the hand-off's current phase, or undefined when the form (or a prerequisite screen) is next.
 * @param options The hand-off state and the frame context.
 * @returns The screen to render, if any.
 */
export const resolveRegistrationHandoffScreen = ({ handoff, subtitle, completionMessage, onStartNew }: HandoffScreenOptions): ReactElement | undefined => {
    if (handoff.errorMessages.length > 0) return <OrganizationSetupErrorScreen subtitle={subtitle} messages={handoff.errorMessages} />;

    // The first durable status read has to complete before it is safe to decide between the form and the
    // waiting phase - showing the form even briefly beforehand would let a recovered, already-submitted
    // registration flash a form it must never resubmit.
    if (!handoff.hasStatus) return <OrganizationSetupResolvingScreen subtitle={subtitle} />;

    if (handoff.phase === 'timedOut') {
        return (
            <OrganizationSetupTimedOutScreen
                subtitle={subtitle}
                onCheckAgain={handoff.checkAgain}
                extraAction={<Button label={strings.registration.startNewRegistration} variant='ghost' onClick={onStartNew} />}
            />
        );
    }

    // A sign-up held for review has been received but is not an organization yet: say so, rather than
    // spinning as if it were about to complete.
    if (handoff.phase === 'heldForReview' || handoff.phase === 'declined') {
        return (
            <OrganizationSetupScreen subtitle={subtitle}>
                <div className='organization-setup-waiting' role='status' aria-live='polite'>
                    <p className='organization-setup-waiting__message'>
                        {reviewMessage(handoff.phase)}
                    </p>
                </div>
            </OrganizationSetupScreen>
        );
    }

    if (handoff.phase === 'hostOutcome') return <OrganizationSetupHostOutcomeScreen subtitle={subtitle} handoff={handoff} succeededMessage={completionMessage} />;

    if (handoff.phase === 'waiting') {
        return <OrganizationSetupSettingUpScreen subtitle={subtitle} message={completionMessage || strings.organizationSetup.settingUp} />;
    }

    return undefined;
};

/**
 * The screen for a prerequisite of the form: the registration start and the legal documents.
 * @param options The start and legal document state and the frame context.
 * @returns The screen to render, if any.
 */
export const resolveRegistrationPrerequisiteScreen = ({ subtitle, startState, onRetryStart, onStartNew, isCheckingLegalDocuments, hasDocuments, onRetryLegalDocuments }: PrerequisiteScreenOptions): ReactElement | undefined => {
    if (startState !== 'started') {
        return (
            <OrganizationSetupScreen subtitle={subtitle}>
                {startState === 'failed'
                    ? <RegistrationStartFailed onRetry={onRetryStart} onStartNew={onStartNew} />
                    : <ProgressSpinner aria-label={strings.registration.starting} />}
            </OrganizationSetupScreen>
        );
    }

    if (isCheckingLegalDocuments && !hasDocuments) return <OrganizationSetupLegalCheckingScreen subtitle={subtitle} />;

    if (!hasDocuments) return <OrganizationSetupLegalUnavailableScreen subtitle={subtitle} onRetry={onRetryLegalDocuments} />;

    return undefined;
};
