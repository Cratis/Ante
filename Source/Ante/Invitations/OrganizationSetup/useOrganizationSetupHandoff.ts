// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useRef, useState } from 'react';
import { Guid } from '@cratis/fundamentals';
import { StatusForInvitation } from './OrganizationSetup';
import { OrganizationSetupAcceptanceStatus } from './OrganizationSetupAcceptanceStatus';
import { HostUrl } from '../../Configuration/Configuration';
import { resolveHostAppRedirectUrl } from '../../Configuration/hostAppRedirect';
import { useOnboardingRecovery } from '../useOnboardingRecovery';
import { OnboardingRecoveryPhase } from '../OnboardingRecoveryPhase';

export type OrganizationSetupHandoffOptions = {
    /** The invitation or registration identifier this setup is for. */
    invitationId: Guid;
    /** Message shown when setup published but the host destination could not be resolved. */
    hostAppUnavailableMessage: string;
};

export type OrganizationSetupHandoffState = {
    /** Whether the durable status for this operation has been read at least once. */
    hasStatus: boolean;
    /** The phase to render: the wizard form, a waiting spinner, or a recoverable timed-out state. */
    phase: OnboardingRecoveryPhase;
    /** Set once setup published but the redirect destination could not be resolved or reached. */
    errorMessages: string[];
    /** Records the organization name at submit time, for the redirect once the host is ready. */
    captureOrganizationName: (organizationName: string) => void;
    /** Call once the wizard's command has succeeded, to switch to the waiting phase immediately. */
    markSubmitted: () => void;
    /** Resets the timeout window without creating a new operation or losing durable status observation. */
    checkAgain: () => void;
};

/**
 * Shared status/hand-off coordination for the two wizards that create an organization -
 * `OrganizationSetupPage` (invited) and `RegistrationPage` (self-service) - which poll the exact same
 * durable status query and redirect the exact same way once it publishes. Extracted here because the two
 * pages already needed byte-identical logic for this, not as a general-purpose abstraction over the
 * three onboarding journeys.
 * @param options The invitation/registration id to track and the message to show on a destination failure.
 * @returns The current phase and the actions the page's `CommandStepper` drives it with.
 */
export const useOrganizationSetupHandoff = ({ invitationId, hostAppUnavailableMessage }: OrganizationSetupHandoffOptions): OrganizationSetupHandoffState => {
    const [statusResult] = StatusForInvitation.use({ invitationId });
    const [hostUrlResult] = HostUrl.use();
    const [errorMessages, setErrorMessages] = useState<string[]>([]);
    const organizationNameRef = useRef('');

    const isRecorded = statusResult.hasData && statusResult.data.status !== OrganizationSetupAcceptanceStatus.pending;
    const isAccepted = statusResult.hasData && statusResult.data.status === OrganizationSetupAcceptanceStatus.accepted;
    const recovery = useOnboardingRecovery(isRecorded, isAccepted);

    useEffect(() => {
        if (!recovery.isAccepted) return;

        if (!hostUrlResult.isSuccess) {
            setErrorMessages([hostAppUnavailableMessage]);
            return;
        }

        const organizationName = statusResult.data.organizationName || organizationNameRef.current;
        if (!organizationName) return;

        try {
            const config = hostUrlResult.data;
            window.location.href = resolveHostAppRedirectUrl(config.hostAppUrl, organizationName, config.signInPath);
        } catch {
            // A malformed host configuration must present as a recoverable destination failure, not a
            // crashed page - setup already published, so the person's information was not lost.
            setErrorMessages([hostAppUnavailableMessage]);
        }
    }, [recovery.isAccepted, statusResult.data.organizationName, hostUrlResult, hostAppUnavailableMessage]);

    return {
        hasStatus: statusResult.hasData,
        phase: recovery.phase,
        errorMessages,
        captureOrganizationName: (organizationName: string) => { organizationNameRef.current = organizationName; },
        markSubmitted: () => recovery.markSubmitted(),
        checkAgain: () => recovery.checkAgain(),
    };
};
